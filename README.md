# stins4cs
## A State Inspection Tool for C# programs
This code base contains two C# projects:

##PatternMatching:

This is a tool that helps in finding regex matches in source code. To use this tool, build the code in visual studio after editing App.config file. The settings of the tool are as follows:
- Code_path: the absolute path to the input code. Example: C:\Users\user\Documents\TestProject
- Regex: the syntax of the regular expression to check. Example: if\s*\(((?!\s*\{).+)\)\s*\{?(.|\s)*?\}?
- Result_path: the path where to output the result of matching (report text file generated). Example:               C:\Users\user\results\
- Sample_name: the name of the test to be written in the report, this is useful when running multiple experiments. Example: sample-regex1-obfuscated.

##Transformer

This is code for stins4cs. This code performs the sequential process presented in the paper[]. To use this tool, build the code in visual studio after editing App.config file. Make sure to have all Roslyn libs within the build path. The settings of the tool are as follows:
- Path_To_Solution: the absolute path to the input solution file (.sln)  of the input code base. This base will be transformed by the tool. Example: C:\pexsamplestest\Samples.Pex.sln
- Project_Name: the name of the project to be transformed. 
- Output_Dir: the path where to output the transformed code. Example: C:\pexsamplestest-2\
- Pex_Report_Path: the path to the Pex report corresponding to the input code base.  Please refer to Pex documentation for the commands to run and generate the reports. Example:   D:\reports\pexsamples\report.per
- Other settings are for the configuration of the transformation, they have default values. For details about them refer to the paper[].


