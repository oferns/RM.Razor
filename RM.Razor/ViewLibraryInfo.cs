namespace RM.Razor {

    public class ViewLibraryInfo {

        /// <summary>
        /// The Assembly Name 
        /// </summary>
        public string AssemblyName { get; set; }


        /// <summary>
        /// The path of the source files. Only used by the runtime compilation feature.
        /// If it is not set or is empty, the runtime compilation feature will not detect
        /// changes to the files in the assembly. If it is not found, then an error will be 
        /// thrown by the IOptionsSetup implementation in this library.        
        /// </summary>
        public string PathRelativeToContentRoot { get; set; }


        /// <summary>
        /// If this is set then an embedded file provider is added to the pipeline 
        /// to serve static files from this ViewLibrary       
        /// </summary>
        public string EmbeddedStaticFilePath { get; set; }

    }
}
