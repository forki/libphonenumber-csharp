#I @"packages\FAKE\"
#r "FakeLib.dll"
#load @".\csharp\lib\PomUtil.fsx"

open Fake
open Fake.AssemblyInfoFile
open Fake.SignHelper

let tags = "c# phonenumbers libphonenumber"

let toolsDir = @".\csharp\lib\"
let buildDir = @".\build\"
let signedDir = @".\build\signed\"
let deployDir = @".\artifacts\"
let testDir =  @".\test\"
let metaDir = @"csharp\PhoneNumbers\"
let keyFile = @".\csharp\PhoneNumbers\key.snk"
let pomPath = @".\java\pom.xml"

let nunitPath = @".\packages\NUnit.Runners\tools"

let appReferences  = !! @"csharp\PhoneNumbers\PhoneNumbers.csproj"     
let testReferences = !! @"csharp\PhoneNumbers.Test\*.csproj"     
let metadataFiles = !! @"paket-files\**\*.xml"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
)

Target "CopyMetadata" (fun _ ->
  metadataFiles |> Seq.iter (CopyFile metaDir)
)

Target "BuildApp" (fun _ ->
    let pomVer = PomUtil.getPomProjectVersion()

    CreateCSharpAssemblyInfo @".\csharp\PhoneNumbers\Properties\AssemblyInfo.cs"
        [ Attribute.Title "PhoneNumbers"
          Attribute.Description "Google's libphonenumber"
          Attribute.Product "PhoneNumbers"
          Attribute.Copyright "Copyright © Google 2015"
          Attribute.Version pomVer
          Attribute.FileVersion pomVer
          Attribute.InternalsVisibleTo "PhoneNumbers.Test" ]

    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output:"
)

Target "BuildTest" (fun _ ->
    MSBuildRelease testDir "Build" testReferences
        |> Log "TestBuild-Output:"
)

Target "Test" (fun _ ->
 !! (testDir + @"\*Test.dll") 
        |> NUnit (fun p -> 
            { p with 
                ToolPath = nunitPath; 
                TimeOut = System.TimeSpan.FromMinutes(60.)
                OutputFile = testDir + @"TestResults.xml" })
)

Target "BuildSigned" (fun _ ->
    CreateCSharpAssemblyInfo @".\csharp\PhoneNumbers\Properties\AssemblyInfo.cs"
        [ Attribute.Title "PhoneNumbers"
          Attribute.Description "Google's libphonenumber"
          Attribute.Product "PhoneNumbers"
          Attribute.Copyright "Copyright © Google 2015"
          Attribute.Version (PomUtil.getPomProjectVersion())
          Attribute.FileVersion (PomUtil.getPomProjectVersion())
          Attribute.KeyFile "key.snk" ]

    MSBuildRelease signedDir "Build"  appReferences 
        |> Log "AppBuild-Output:"   
)

  
Target "All" DoNothing

"Clean"
    ==> "CopyMetadata"
    ==> "BuildApp"
    ==> "BuildTest"
    ==> "Test"
    ==> "BuildSigned"
    ==> "All"

Run <| getBuildParamOrDefault "target" "All"