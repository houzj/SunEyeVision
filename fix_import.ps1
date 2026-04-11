 = Get-Content 'd:/MyWork/SunEyeVision_Dev-camera/Src/UI/ViewModels/CameraDetailViewModel.cs' -Raw -Encoding UTF8
 =  -replace '(using System;
using System.ComponentModel;)', 'using System;
using System.ComponentModel;
using System.Windows.Input;'
[System.IO.File]::WriteAllText('d:/MyWork/SunEyeVision_Dev-camera/Src/UI/ViewModels/CameraDetailViewModel.cs', , [System.Text.UTF8Encoding]::new($false))
