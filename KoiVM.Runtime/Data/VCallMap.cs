#region

using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.VCalls;

#endregion

namespace System.Runtime.Serialization.Formatters.Data
{
    internal static class VCallMap
    {
        private static readonly Dictionary<byte, IVCall> vCalls;

        static VCallMap()
        {
            vCalls = new Dictionary<byte, IVCall>();
            foreach(var type in typeof(VCallMap).Assembly.GetTypes())
                if(typeof(IVCall).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var vCall = (IVCall) Activator.CreateInstance(type);
                    vCalls[vCall.Code] = vCall;
                }
        }

        public static IVCall Lookup(byte code)
        {
            return vCalls[code];
        }
    }
}