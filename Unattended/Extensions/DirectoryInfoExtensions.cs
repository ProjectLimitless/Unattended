/** 
* This file is part of Unattended.
* Copyright © 2016 Donovan Solms.
* Project Limitless
* https://www.projectlimitless.io
* 
* Unattended and Project Limitless is free software: you can redistribute it and/or modify
* it under the terms of the Apache License Version 2.0.
* 
* You should have received a copy of the Apache License Version 2.0 with
* Unattended. If not, see http://www.apache.org/licenses/LICENSE-2.0.
*/

using System.IO;

namespace Limitless.Unattended.Extensions
{
    /// <summary>
    /// Defines extensions for DirectoryInfo instances.
    /// </summary>
    public static class DirectoryInfoExtensions
    {
        /// <summary>
        /// Copies this directory (and all sub-directories) to the destination directory.
        /// 
        /// Adapted from MSDN https://msdn.microsoft.com/en-us/library/bb762914(v=vs.110).aspx.
        /// </summary>
        /// <param name="source">The directory to copy</param>
        /// <param name="destination">The directory to copy to</param>
        /// <exception cref="DirectoryNotFoundException"/>
        public static void DeepCopyTo(this DirectoryInfo source, string destination)
        {
            if (source.Exists == false)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + source.FullName);
            }

            DirectoryInfo[] dirs = source.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destination, file.Name);
                file.CopyTo(temppath, false);
            }
            // And all the subdirectories
            foreach (DirectoryInfo subdirectory in dirs)
            {
                string newPath = Path.Combine(destination, subdirectory.Name);
                subdirectory.DeepCopyTo(newPath);
            }
        }
    }
}
