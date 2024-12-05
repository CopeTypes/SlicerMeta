using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using SlicerMeta.parser.gcode;

namespace SlicerMeta.parser.threemf
{
    // Parser for 3MF files, tested with a file sliced by Orca-FlashForge
    // Should work on regular OrcaSlicer, and possibly other's as long as the format doesn't vary
    public class ThreeMfParser
    {
        
        public string PrinterModelId { get; private set; }
        public bool SupportUsed { get; private set; }
        public List<string> FileNames { get; private set; } = new List<string>();
        public List<FilamentInfo> Filaments { get; private set; } = new List<FilamentInfo>();
        public Image PlateImage { get; private set; }
        
        public SlicerMeta SlicerInfo { get; private set; }
        public SlicerFileMeta FileInfo { get; private set; }


        public ThreeMfParser()
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            PrinterModelId = "Unknown";
            SupportUsed = false;
            PlateImage = null;
        }

        public ThreeMfParser Parse(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                ParseSliceInfoConfig(archive); // Parse slice_info.config
                ExtractPlateImage(archive); // Extract plate_1.png
                ParseGCodeInfo(archive);
            }

            return this;
        }
        
        
        private void ParseGCodeInfo(ZipArchive archive)
        { // Extract gcode metadata from the embedded file
            var gcodeEntry = archive.GetEntry("Metadata/plate_1.gcode");
            if (gcodeEntry == null)
            {
                Debug.WriteLine("Unable to find plate_1.gcode");
                return;
            }
            using (var configStream = gcodeEntry.Open())
            {
                var meta = new GCodeParser().ParseFromStream(configStream);
                SlicerInfo = meta.SlicerInfo;
                FileInfo = meta.FileInfo;
            }
        }
        
        
        private void ParseSliceInfoConfig(ZipArchive archive)
        {
            var configEntry = archive.GetEntry("Metadata/slice_info.config");
            if (configEntry != null)
            {
                using (var configStream = configEntry.Open())
                {
                    var xmlDoc = XDocument.Load(configStream);
                    ParseConfigXml(xmlDoc);
                }
            }
            else Debug.WriteLine("slice_info.config file not found in the 3MF archive.");
        }

        private void ExtractPlateImage(ZipArchive archive)
        {
            var imageEntry = archive.GetEntry("Metadata/plate_1.png");
            if (imageEntry != null)
            {
                using (var imageStream = imageEntry.Open())
                {
                    using (var ms = new MemoryStream())
                    {
                        imageStream.CopyTo(ms);
                        ms.Position = 0;
                        PlateImage = Image.FromStream(ms);
                    }
                }
            }
            else Debug.WriteLine("plate_1.png file not found in the 3MF archive.");
        }
        
        private void ParseConfigXml(XContainer xmlDoc)
        { // todo cleanup
            var configElement = xmlDoc.Element("config");
            if (configElement != null)
            {
                var plateElement = configElement.Element("plate");
                if (plateElement != null)
                {
                    // Parse the printer name (i think that's what this always is?)
                    var printerModelIdElement = plateElement.Elements("metadata").FirstOrDefault(e => e.Attribute("key")?.Value == "printer_model_id");
                    if (printerModelIdElement != null) PrinterModelId = printerModelIdElement.Attribute("value")?.Value;

                    // Parse support used bool
                    var supportUsedElement = plateElement.Elements("metadata").FirstOrDefault(e => e.Attribute("key")?.Value == "support_used");
                    if (supportUsedElement != null)
                    {
                        var supportUsedValue = supportUsedElement.Attribute("value")?.Value;
                        if (bool.TryParse(supportUsedValue, out var supportUsed)) SupportUsed = supportUsed;
                        else Debug.WriteLine("Invalid value for support_used.");
                    }

                    // Parse file name(s) from object tag(s)
                    var objectElements = plateElement.Elements("object");
                    foreach (var objectElement in objectElements)
                    {
                        var fileName = objectElement.Attribute("name")?.Value;
                        if (!string.IsNullOrEmpty(fileName)) FileNames.Add(fileName);
                    }

                    // Parse filament info for filament(s)
                    var filamentElements = plateElement.Elements("filament");
                    foreach (var filamentElement in filamentElements)
                    {
                        var filament = new FilamentInfo
                        {
                            Id = filamentElement.Attribute("id")?.Value,
                            Type = filamentElement.Attribute("type")?.Value,
                            Color = filamentElement.Attribute("color")?.Value,
                            UsedM = filamentElement.Attribute("used_m")?.Value,
                            UsedG = filamentElement.Attribute("used_g")?.Value,
                        };
                        Filaments.Add(filament);
                    }
                }
                else Debug.WriteLine("<plate> element not found in slice_info.config.");
            }
            else Debug.WriteLine("Invalid slice_info.config file.");
        }
    }
}