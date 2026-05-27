**DarksVM - KoiVM custom**
========
**DarksVM** is a modified version of **KoiVM** which is a ConfuserEx plugin that allows you to virtualize methods to be understandable only by our computer.

This version includes:
* Modified VMEntry name and entries
* Renamed 'Run' method to 'Load'
* Added some calculation
* OldRod is no longer able to devirtualize
* Improved compatibility
* Only supported OS is Windows.

![](_68747470733a2f2f63646e2e6434726b2e66722f4d486d2e6a7067.png)

**Building on macOS**
--------
1. Install Mono: `brew install mono`
2. Build in order:
   ```
   xbuild KoiVM.Runtime/KoiVM.Runtime.csproj /p:Configuration=Debug /p:Platform=AnyCPU
   xbuild KoiVM/KoiVM.csproj /p:Configuration=Debug /p:Platform=AnyCPU
   xbuild KoiVM.Confuser/KoiVM.Confuser.csproj /p:Configuration=Debug /p:Platform=AnyCPU
   ```
   Output goes to `bin/` (KoiVM.Confuser requires ConfuserEx dependencies).

**How to use**
--------
Add these projects to your ConfuserEx, then add this in your .crproj project file
```
<rule pattern="true" inherit="false">
  <protection id="virt" />
</rule>
<plugin>?:\path\to\your\project\KoiVM.Confuser.exe</plugin>
```

Credit to d4rk, developer and creator.
Join the Discord community and get access to DarkProtector and others, for free!
https://discord.gg/bkkybeM







![](d4rk_avatar.gif?s=87)
