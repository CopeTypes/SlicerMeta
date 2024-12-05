using System.Drawing;
using System.Net.Mime;

namespace SlicerMeta
{
    // Global metadata for the file itself
    public class SlicerFileMeta
    {
        public Image Thumbnail { get; set; }
        public double FilamentUsedMM { get; set; }
        public double FilamentUsedG { get; set; }
        public string FilamentType { get; set; }
        public string PrinterModel { get; set; }
        
        public SlicerType SliceSoft { get; private set; }

        public SlicerFileMeta()
        {
            SetDefaults();
        }

        public SlicerFileMeta FromFile(SlicerType slicerType, string filePath)
        {
            SliceSoft = slicerType;

            return this;
        }

        private void SetDefaults()
        {
            Thumbnail = null;
            FilamentUsedMM = 0.0D;
            FilamentUsedG = 0.0D;
            FilamentType = "Unknown";
            PrinterModel = "Unknown";
        }
        
    }
    
}