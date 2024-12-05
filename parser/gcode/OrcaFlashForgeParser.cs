﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace SlicerMeta.parser.gcode
{
    // Parse GCode files sliced by Orca-FlashForge
    public class OrcaFlashForgeParser
    {
        
        public static (SlicerMeta, SlicerFileMeta) Parse(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                var inHeaderBlock = false;
                var inThumbnailBlock = false;
                var inThumbnailData = false;
                var inConfigBlock = false;
                var thumbnailDataBuilder = new StringBuilder();

                var slicerMeta = new SlicerMeta();
                var fileMeta = new SlicerFileMeta();
                
                while ((line = reader.ReadLine()) != null)
                {
                    // Trim leading and trailing whitespace
                    line = line.Trim();

                    switch (line)
                    {
                        // Check for HEADER_BLOCK_START and END
                        case "; HEADER_BLOCK_START":
                            inHeaderBlock = true;
                            continue;
                        case "; HEADER_BLOCK_END":
                            inHeaderBlock = false;
                            continue;
                        // Check for THUMBNAIL_BLOCK_START and END
                        case "; THUMBNAIL_BLOCK_START":
                            inThumbnailBlock = true;
                            continue;
                        case "; THUMBNAIL_BLOCK_END":
                            inThumbnailBlock = false;
                            continue;
                        // Check for CONFIG_BLOCK_START and END
                        case "; CONFIG_BLOCK_START":
                            inConfigBlock = true;
                            continue;
                        case "; CONFIG_BLOCK_END":
                            inConfigBlock = false;
                            continue;
                    }

                    // Parse header data
                    if (inHeaderBlock) ParseHeaderLine(line, slicerMeta);
                    else if (inThumbnailBlock)
                    {
                        // Parse thumbnail data
                        if (line.StartsWith("; thumbnail begin"))
                        {
                            inThumbnailData = true;
                            continue;
                        }

                        if (line == "; thumbnail end")
                        {
                            inThumbnailData = false;
                            // Process the collected thumbnail data
                            ProcessThumbnailData(thumbnailDataBuilder.ToString(), fileMeta);
                            thumbnailDataBuilder.Clear();
                            continue;
                        }

                        if (!inThumbnailData) continue;
                        var base64Line = line.TrimStart(';', ' ');
                        thumbnailDataBuilder.Append(base64Line);
                    }
                    else if (inConfigBlock) ParseConfigLine(line, fileMeta);
                    else ParseAdditionalData(line, fileMeta);
                }
                return (slicerMeta, fileMeta);
            }
        }
        
        private static void ParseHeaderLine(string line, SlicerMeta meta)
        {
            // Remove leading semicolons and whitespace
            line = line.TrimStart(';', ' ');
            
            if (line.StartsWith("generated by"))
            { // parse slicer meta data
                meta.FromString(SlicerType.OrcaFF, line);
                return;
            }

            if (line.StartsWith("model printing time"))
            { // parse estimated print time 
                meta.SetEta(line);
                return;
            }
            
            // Split the line into key and value
            //var separatorIndex = line.IndexOf(':');
            //if (separatorIndex <= -1) return;
            //var key = line.Substring(0, separatorIndex).Trim();
            //var value = line.Substring(separatorIndex + 1).Trim();
            //HeaderData[key] = value;
        }
        
        private static void ParseConfigLine(string line, SlicerFileMeta fileMeta)
        {
            // Remove leading semicolons and whitespace
            line = line.TrimStart(';', ' ');

            // Split the line into key and value
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= -1)
            {
                fileMeta.FilamentType = "Unknown";
                fileMeta.PrinterModel = "Unknown";
                return;
            }
            var key = line.Substring(0, equalsIndex).Trim();
            var value = line.Substring(equalsIndex + 1).Trim();

            switch (key)
            {
                case "filament_type":
                    fileMeta.FilamentType = value;
                    break;
                case "printer_model":
                    fileMeta.PrinterModel = value;
                    break;
            }
        }

        private static void ParseAdditionalData(string line, SlicerFileMeta fileMeta)
        {
            // Check if the line starts with a semicolon
            if (!line.StartsWith(";")) return;
            // Remove leading semicolons and whitespace
            line = line.TrimStart(';', ' ');

            // Split the line into key and value
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= -1) return;
            var key = line.Substring(0, equalsIndex).Trim();
            var value = line.Substring(equalsIndex + 1).Trim();

            switch (key)
            {
                case "filament used [mm]":
                {
                    if (double.TryParse(value, out var filamentUsedMM)) fileMeta.FilamentUsedMM = filamentUsedMM;
                    break;
                }
                case "filament used [g]":
                {
                    if (double.TryParse(value, out var filamentUsedG)) fileMeta.FilamentUsedG = filamentUsedG;
                    break;
                }
            }
        }

        private static void ProcessThumbnailData(string base64Data, SlicerFileMeta fileMeta)
        {
            try
            {
                var imageBytes = Convert.FromBase64String(base64Data);
                using (var ms = new MemoryStream(imageBytes)) fileMeta.Thumbnail = Image.FromStream(ms);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decoding thumbnail image: {ex.Message}");
            }
        }
    }
}