#region

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Emit;
using KoiVM.AST.IL;
using KoiVM.CFG;
using KoiVM.RT;
using KoiVM.VM;

#endregion

namespace KoiVM.VMIL.Transforms
{
    public class BlockKeyTransform : IPostTransform
    {
        private Dictionary<ILBlock, BlockKey> Keys;
        private NeonVMMethodInfo methodInfo;

        private NeonVMRuntime runtime;

        public void Initialize(ILPostTransformer tr)
        {
            runtime = tr.Runtime;
            methodInfo = tr.Runtime.Descriptor.Data.LookupInfo(tr.Method);
            Console.WriteLine("[BLOCKKEY-INIT] method=" + tr.Method.Name + " EntryKey=0x" + methodInfo.EntryKey.ToString("x8"));
            ComputeBlockKeys(tr.RootScope);
            // Dump final keys
            foreach(var kv in Keys)
                Console.WriteLine("[BLOCKKEY-FINAL] blockId=" + kv.Key.Id + " method=" + tr.Method.Name +
                    " Entry=0x" + kv.Value.Entry.ToString("x8") + " Exit=0x" + kv.Value.Exit.ToString("x8"));
        }

        public void Transform(ILPostTransformer tr)
        {
            var key = Keys[tr.Block];
            methodInfo.BlockKeys[tr.Block] = new VMBlockKey
            {
                EntryKey = key.Entry,
                ExitKey = key.Exit
            };
        }

        private void ComputeBlockKeys(ScopeBlock rootScope)
        {
            var blocks = rootScope.GetBasicBlocks().OfType<ILBlock>().ToList();
            Console.WriteLine("[BLOCKKEY] blocks=" + blocks.Count);
            uint id = 1;
            Keys = blocks.ToDictionary(
                block => block,
                block => new BlockKey {Entry = id++, Exit = id++});
            var ehMap = MapEHs(rootScope);

            // Log initial state
            for(int bi = 0; bi < blocks.Count; bi++)
                Console.WriteLine("[BLOCKKEY-INITIAL] [" + bi + "] blockId=" + blocks[bi].Id +
                    " sources=" + blocks[bi].Sources.Count + " targets=" + blocks[bi].Targets.Count +
                    " Entry=0x" + Keys[blocks[bi]].Entry.ToString("x8") + " Exit=0x" + Keys[blocks[bi]].Exit.ToString("x8"));

            bool updated;
            do
            {
                updated = false;

                BlockKey key;

                key = Keys[blocks[0]];
                key.Entry = 0xfffffffe;
                Keys[blocks[0]] = key;

                key = Keys[blocks[blocks.Count - 1]];
                key.Exit = 0xfffffffd;
                Keys[blocks[blocks.Count - 1]] = key;

                // Update the state ids with the maximum id
                foreach(var block in blocks)
                {
                    key = Keys[block];
                    if(block.Sources.Count > 0)
                    {
                        var newEntry = block.Sources.Select(b => Keys[(ILBlock) b].Exit).Max();
                        if(key.Entry != newEntry)
                        {
                            key.Entry = newEntry;
                            updated = true;
                        }
                    }
                    if(block.Targets.Count > 0)
                    {
                        var newExit = block.Targets.Select(b => Keys[(ILBlock) b].Entry).Max();
                        if(key.Exit != newExit)
                        {
                            key.Exit = newExit;
                            updated = true;
                        }
                    }
                    Keys[block] = key;
                }

                // Match finally enter = finally exit = try end exit
                // Match filter start = 0xffffffff
                MatchHandlers(ehMap, ref updated);
            } while(updated);

            // Replace id with actual values
            // All blocks across ALL methods share the same multiplier/inverse multiplier
            // so JMP/CALL/RET fixup bytes (which only adjust byte 0) can correctly transition keys.
            byte sharedMult = runtime.Descriptor.Data.globalMult;
            byte sharedInv = runtime.Descriptor.Data.globalInv;
            Console.WriteLine("[BLOCKKEY-REPLACE] EntryKey=0x" + methodInfo.EntryKey.ToString("x8") + " sharedMult=0x" + sharedMult.ToString("x2") + " sharedInv=0x" + sharedInv.ToString("x2"));

            var idMap = new Dictionary<uint, uint>();
            idMap[0xffffffff] = 0;
            idMap[0xfffffffe] = methodInfo.EntryKey;
            idMap[0xfffffffd] = methodInfo.ExitKey;
            foreach(var block in blocks)
            {
                var key = Keys[block];

                var entryId = key.Entry;
                if(!idMap.TryGetValue(entryId, out key.Entry))
                {
                    uint rolling = Entropy.DeriveByte(runtime.Descriptor.Settings.Seed, entryId, "block_entry");
                    key.Entry = idMap[entryId] = rolling | ((uint)sharedMult << 8) | ((uint)sharedInv << 16);
                    Console.WriteLine("[BLOCKKEY-NEW] blockId=" + block.Id + " entryId=" + entryId + " rolling=0x" + rolling.ToString("x2") + " mult=0x" + sharedMult.ToString("x2") + " result=0x" + key.Entry.ToString("x8"));
                }
                else
                {
                    Console.WriteLine("[BLOCKKEY-CACHED] blockId=" + block.Id + " entryId=" + entryId + " cached=0x" + key.Entry.ToString("x8"));
                }

                var exitId = key.Exit;
                if(!idMap.TryGetValue(exitId, out key.Exit))
                {
                    uint rolling = Entropy.DeriveByte(runtime.Descriptor.Settings.Seed, exitId, "block_exit");
                    key.Exit = idMap[exitId] = rolling | ((uint)sharedMult << 8) | ((uint)sharedInv << 16);
                }

                Keys[block] = key;
            }
        }

        private EHMap MapEHs(ScopeBlock rootScope)
        {
            var map = new EHMap();
            MapEHsInternal(rootScope, map);
            return map;
        }

        private void MapEHsInternal(ScopeBlock scope, EHMap map)
        {
            if(scope.Type == ScopeType.Filter)
            {
                map.Starts.Add((ILBlock) scope.GetBasicBlocks().First());
            }
            else if(scope.Type != ScopeType.None)
            {
                if(scope.ExceptionHandler.HandlerType == ExceptionHandlerType.Finally)
                {
                    FinallyInfo info;
                    if(!map.Finally.TryGetValue(scope.ExceptionHandler, out info))
                        map.Finally[scope.ExceptionHandler] = info = new FinallyInfo();

                    if(scope.Type == ScopeType.Try)
                    {
                        // Try End Next
                        var scopeBlocks = new HashSet<IBasicBlock>(scope.GetBasicBlocks());
                        foreach(ILBlock block in scopeBlocks)
                            if((block.Flags & BlockFlags.ExitEHLeave) != 0 &&
                               (block.Targets.Count == 0 ||
                                block.Targets.Any(target => !scopeBlocks.Contains(target))))
                                foreach(var target in block.Targets)
                                    info.TryEndNexts.Add((ILBlock) target);
                    }
                    else if(scope.Type == ScopeType.Handler)
                    {
                        // Finally End
                        IEnumerable<IBasicBlock> candidates;
                        if(scope.Children.Count > 0)
                            candidates = scope.Children
                                .Where(s => s.Type == ScopeType.None)
                                .SelectMany(s => s.GetBasicBlocks());
                        else candidates = scope.Content;
                        foreach(ILBlock block in candidates)
                            if((block.Flags & BlockFlags.ExitEHReturn) != 0 &&
                               block.Targets.Count == 0) info.FinallyEnds.Add(block);
                    }
                }
                if(scope.Type == ScopeType.Handler)
                    map.Starts.Add((ILBlock) scope.GetBasicBlocks().First());
            }
            foreach(var child in scope.Children)
                MapEHsInternal(child, map);
        }

        private void MatchHandlers(EHMap map, ref bool updated)
        {
            // handler start = 0xffffffff
            // finally end = next block of try end
            foreach(var start in map.Starts)
            {
                var key = Keys[start];
                if(key.Entry != 0xffffffff)
                {
                    key.Entry = 0xffffffff;
                    Keys[start] = key;
                    updated = true;
                }
            }
            foreach(var info in map.Finally.Values)
            {
                var maxEnd = info.FinallyEnds.Max(block => Keys[block].Exit);
                var maxEntry = info.TryEndNexts.Max(block => Keys[block].Entry);
                var maxId = Math.Max(maxEnd, maxEntry);

                foreach(var block in info.FinallyEnds)
                {
                    var key = Keys[block];
                    if(key.Exit != maxId)
                    {
                        key.Exit = maxId;
                        Keys[block] = key;
                        updated = true;
                    }
                }

                foreach(var block in info.TryEndNexts)
                {
                    var key = Keys[block];
                    if(key.Entry != maxId)
                    {
                        key.Entry = maxId;
                        Keys[block] = key;
                        updated = true;
                    }
                }
            }
        }

        private struct BlockKey
        {
            public uint Entry;
            public uint Exit;
        }

        private class FinallyInfo
        {
            public readonly HashSet<ILBlock> FinallyEnds = new HashSet<ILBlock>();
            public readonly HashSet<ILBlock> TryEndNexts = new HashSet<ILBlock>();
        }

        private class EHMap
        {
            public readonly Dictionary<ExceptionHandler, FinallyInfo> Finally = new Dictionary<ExceptionHandler, FinallyInfo>();
            public readonly HashSet<ILBlock> Starts = new HashSet<ILBlock>();
        }
    }
}