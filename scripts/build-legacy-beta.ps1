$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$refs=$env:FrameworkPathOverride
if(!$refs -or !(Test-Path (Join-Path $refs 'mscorlib.dll'))){throw 'FrameworkPathOverride missing .NET 3.5 references.'}
$msbuild=(Get-Command MSBuild.exe -ErrorAction Stop).Source
& $PSScriptRoot\build-legacy-core.ps1 -FrameworkPathOverride $refs
if($LASTEXITCODE -ne 0){throw 'Legacy Core compile failed.'}
$projects=@('src\ITDigitacao.Legacy.App\ITDigitacao.Legacy.App.csproj','src\ITDigitacao.Legacy.Shortcut\Criar-Atalho.csproj')
foreach($project in $projects){& $msbuild (Join-Path $root $project) /t:Rebuild /p:Configuration=Release /p:Platform=x86 "/p:FrameworkPathOverride=$refs" /nologo;if($LASTEXITCODE -ne 0){throw "Build failed: $project"}}
$app=Join-Path $root 'src\ITDigitacao.Legacy.App\bin\x86\Release'
$shortcut=Join-Path $root 'src\ITDigitacao.Legacy.Shortcut\bin\x86\Release'
$core=Join-Path $root 'src\ITDigitacao.Legacy.Core\bin\x86\Release\ITDigitacao.Legacy.Core.dll'
foreach($item in @((Join-Path $app 'IT-Digitacao.exe'),(Join-Path $shortcut 'Criar-Atalho.exe'),$core,(Join-Path $app 'atividades.json'),(Join-Path $app 'Assets\mascote-boas-vindas.png'),(Join-Path $app 'Assets\Categorias\tecnologia.png'))){if(!(Test-Path $item)){throw "Required fresh build output/resource missing: $item"}}
$catalog=Get-Content (Join-Path $app 'atividades.json') -Raw | ConvertFrom-Json
if($catalog.Categories.Count -ne 10 -or $catalog.Activities.Count -ne 50){throw "Catalog integrity check failed: categories=$($catalog.Categories.Count), activities=$($catalog.Activities.Count)"}
$categoryImages=@(Get-ChildItem (Join-Path $app 'Assets\Categorias') -Filter '*.png' -File)
if($categoryImages.Count -ne 10){throw "Expected 10 category illustrations in build output, found $($categoryImages.Count)"}
$publish=Join-Path $root 'legacy-beta-stage'
if(Test-Path $publish){Remove-Item $publish -Recurse -Force}
New-Item -ItemType Directory -Path $publish | Out-Null
Copy-Item (Join-Path $app 'IT-Digitacao.exe') $publish
Copy-Item (Join-Path $shortcut 'Criar-Atalho.exe') $publish
Copy-Item $core $publish
Copy-Item (Join-Path $app 'atividades.json') $publish
Copy-Item (Join-Path $app 'Assets') (Join-Path $publish 'Assets') -Recurse
Copy-Item (Join-Path $root 'docs\legacy\LEIA-ME.txt') $publish
Copy-Item (Join-Path $root 'docs\legacy\LIMITACOES-BETA.txt') $publish
$assemblies=@((Join-Path $publish 'IT-Digitacao.exe'),(Join-Path $publish 'Criar-Atalho.exe'),(Join-Path $publish 'ITDigitacao.Legacy.Core.dll'))
$coreIdentity=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $publish 'ITDigitacao.Legacy.Core.dll'))
if($coreIdentity.Name -ne 'ITDigitacao.Legacy.Core'){throw "Unexpected Core identity: $($coreIdentity.Name)"}
$frameworkVersions=@{
 'mscorlib'='2.0.0.0'; 'System'='2.0.0.0'; 'System.Core'='3.5.0.0';
 'WindowsBase'='3.0.0.0'; 'PresentationCore'='3.0.0.0'; 'PresentationFramework'='3.0.0.0';
 'System.Runtime.Serialization'='3.0.0.0'; 'System.ServiceModel.Web'='3.5.0.0'
}
foreach($file in $assemblies){
 $asm=[Reflection.Assembly]::ReflectionOnlyLoadFrom($file)
 if($asm.ImageRuntimeVersion -ne 'v2.0.50727'){throw "$file targets $($asm.ImageRuntimeVersion), expected CLR 2.0"}
 if($asm.GetName().ProcessorArchitecture -ne 'X86'){throw "$file is not x86"}
 foreach($reference in $asm.GetReferencedAssemblies()){
  if($reference.Name -eq 'ITDigitacao.Legacy.Core'){
   if($reference.Name -ne $coreIdentity.Name -or $reference.Version -ne $coreIdentity.Version){throw "$file references a mismatched Core identity $($reference.FullName)"}
   continue
  }
  if(!$frameworkVersions.ContainsKey($reference.Name)){throw "$file has unsupported external dependency $($reference.Name)"}
  if($reference.Version.ToString() -ne $frameworkVersions[$reference.Name]){throw "$file references unexpected framework assembly version $($reference.FullName)"}
 }
}
$sha=(Get-FileHash (Join-Path $publish 'IT-Digitacao.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
$info=[ordered]@{Product='IT Digitação Legacy';Version='2.0.0-beta.1';Commit=(git -C $root rev-parse HEAD);Framework='.NET Framework 3.5';CLR='v2.0.50727';Architecture='x86';ApplicationSha256=$sha;BuiltUtc=[DateTime]::UtcNow.ToString('o')}
$info|ConvertTo-Json|Set-Content (Join-Path $publish 'BUILD-INFO.json') -Encoding UTF8
$zip=Join-Path $root 'IT-Digitacao-Legacy-2.0.0-beta.1-Portable-x86.zip'
if(Test-Path $zip){Remove-Item $zip -Force}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($publish,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$extract=Join-Path $env:RUNNER_TEMP 'legacy-beta-integrity';if(Test-Path $extract){Remove-Item $extract -Recurse -Force};[IO.Compression.ZipFile]::ExtractToDirectory($zip,$extract)
foreach($file in Get-ChildItem $publish -File -Recurse){
 $relative=$file.FullName.Substring($publish.Length).TrimStart([char[]]@('\','/'))
 $unzipped=Join-Path $extract $relative
 if(!(Test-Path $unzipped)){throw "ZIP is missing $relative"}
 if((Get-FileHash $file.FullName).Hash -ne (Get-FileHash $unzipped).Hash){throw "ZIP integrity mismatch: $relative"}
}
Write-Host "Created validated package $zip"
