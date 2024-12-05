using System;

namespace SlicerMeta.parser
{
    // Global utils for use wherever in parser code
    public class ParserHelper
    {
        public static string ParseSliceTime(string sliceTime)
        { // parse the 12:34:56 24h format string -> 12:34pm (for gcode files)
            try
            {
                return DateTime.ParseExact(sliceTime, "HH:mm:ss", null).ToString("h:mm tt");
            }
            catch (Exception e)
            {
                Console.WriteLine("ParserHelper annot parse invalid slice time - " + sliceTime + "\n" + e);
                return "Error";
            }
        }
    }
}