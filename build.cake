#tool "xunit.runner.console"

// parse arguments
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildLabel = Argument("buildLabel", string.Empty);
var buildInfo = Argument("buildInfo", string.Empty);

// Parse release notes.
var releaseNotes = ParseReleaseNotes("./ReleaseNotes.md");

// Set version.
var version = releaseNotes.Version.ToString();
var semVersion = version + (buildLabel != "" ? ("-" + buildLabel) : string.Empty);
Information("Building version {0} of Ferret.", version);

// Define directories.
var buildResultDir = "./build";
var testResultsDir = buildResultDir + "/test-results";

//////////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
{
	CleanDirectories(new DirectoryPath[] {
		buildResultDir, testResultsDir });    
});

Task("Patch-Assembly-Info")
	.Description("Patches the AssemblyInfo files.")
	.IsDependentOn("Clean")
	.Does(() =>
{
	var file = "./src/SolutionInfo.cs";
	CreateAssemblyInfo(file, new AssemblyInfoSettings {
		Product = "Ferret",
		Version = version,
		FileVersion = version,
		InformationalVersion = (version + buildInfo).Trim(),
		Copyright = "Copyright (c) Jeff Doolittle " + DateTime.Now.Year,
		Description = "Domain extensions for JasperFx/Marten"
	});
});

Task("Build-Solution")
	.IsDependentOn("Patch-Assembly-Info")
	.Does(() =>
{
	MSBuild("./src/Ferret.sln", s => 
		{ 
			s.Configuration = configuration;
		});
});

Task("Run-Unit-Tests")
	.IsDependentOn("Build-Solution")
	.Does(() =>
{
	XUnit2("./src/**/bin/" + configuration + "/*.Specs.dll", new XUnit2Settings {
		OutputDirectory = testResultsDir,
		XmlReport = true,
		HtmlReport = true,
		Parallelism = ParallelismOption.None,
		MaxThreads = 1
	});
});

Task("Default")
	.Description("Final target.")
	.IsDependentOn("Run-Unit-Tests");

//////////////////////////////////////////////////////////////////////////

RunTarget(target);