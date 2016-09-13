namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FuncNet")>]
[<assembly: AssemblyProductAttribute("FuncNet")>]
[<assembly: AssemblyDescriptionAttribute("A functional approach to a .NET network framework")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
    let [<Literal>] InformationalVersion = "1.0"
