# SlicerMeta
C# API for extracting metadata from G-Code/3MF Files, designed for use with FlashForge (3d printer) software development

# Features
-  Retrieve slicer metadata (name, version, data & time, printer name, etc.) from all gcode files
-  Retrieve filament info (material type) from all gcode & 3mf files. Extra data is available (color, used grams, used meters) depending on slicing software
-  Retreive embedded thumbnails from supported file types (3mf) and suported slicers (gcode)

# Supported Slicers
-  Orca-FlashForge
-  OrcaSlicer (not fully tested)
-  FlashPrint (Flashforge's legacy slicer)

# Example Usage
```csharp
var parser = new GCodeParser(); // create a new instance
parser.Parse(filePath); // path to gcode file

Console.WriteLine($"Sliced by: {parser.SlicerInfo.SlicerName}");  // get slicer name
Console.WriteLine($"Filament Type: {parser.FileInfo.FilamentType}"); // get filament type
```
