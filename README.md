# RM.Razor
Multitenanted Razor Engine for .NET 5 & .NET 6





This library allows you to use multiple Razor Class Libraries in a single ASP.NET project. It works with MVC and RazorPages. 


## What does it do?

It allows you to have multiple views in different libraries for the same path in youur ASP.NET application, and allows you to choose which view to use based on request parameters.
For example, you could use this library to serve different razor views to different clients based on hostname, or requested culture, or any other variable.
