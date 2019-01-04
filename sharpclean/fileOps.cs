﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ImageMagick;
using System.Drawing;


namespace sharpclean
{
    // Handles file operations for the program, including opening and saving images, locating paths, and user interfacing
    public class fileOps
    {
        // Initialize the values for paths
        string dirPath;
        string imgPath;
        string trajPath;
        string offsetPath;
        string tempPath;
        string[] dirFiles;

        public struct coords
        {
            public decimal x, y;
        }

        public coords park, dock, globaloffset;

        #region Public member functions

        // Constructor
        public fileOps()
        {
            dirPath = "";
            imgPath = "";
            trajPath = "";
            offsetPath = "";
        }

        #region Get initial files

        public string getImage()
        {
            // Open the file dialog and save the image and directory paths
            using (OpenFileDialog openFD = new OpenFileDialog())
            {
                // File dialog settings
                openFD.Title = "Select an image file";
                openFD.InitialDirectory = "c:\\Users\\";
                openFD.Filter = "png files (*.png)|*.png";
                openFD.RestoreDirectory = true;

                DialogResult result = openFD.ShowDialog();

                // Only save the file and directory path if the user selects "OK"
                if (result == DialogResult.OK)
                {
                    this.imgPath = openFD.FileName;
                    return this.imgPath;
                }
                else
                {
                    Console.WriteLine("No map selected!");
                    return "err::no_map_selected";
                }
            }
        }

        public string getDir()
        {
            // Gets and sets the directory name
            this.dirPath = Path.GetDirectoryName(this.imgPath);

            // Load files now that the directory has been found
            loadFiles();

            return this.dirPath;
        }

        public string getTraj() // Gets the full trajectory file path
        {
            this.trajPath = getFilebyType(".ply");
            return this.trajPath;
        }

        public string getOffset() // Gets the full info file path
        {
            this.offsetPath = getFilebyType(".info");
            return this.offsetPath;
        }

        #endregion

        public void getParkandDock()
        {
            // open offset file, read to the 5th line and save the offset
            StreamReader offsetfile = new StreamReader(offsetPath);

            for (int i = 0; i < 5; i++)
                offsetfile.ReadLine();

            string[] ss;
            string streamline = offsetfile.ReadLine();

            if (streamline.Contains(":"))
            {
                ss = streamline.Split();
                globaloffset.x = Convert.ToDecimal(ss[1]);
                globaloffset.y = Convert.ToDecimal(ss[2]);
            }

            // open trajectory file, read to 'end_header' line while checking for 'element', if found then save vertex count
            int vertexes = 0;

            StreamReader trajfile = new StreamReader(trajPath);

            while ((streamline = trajfile.ReadLine()) != "end_header")
            {
                if ((ss = streamline.Split())[0] == "element")
                    vertexes = Convert.ToInt16(ss[2]);
            }

            // store first two columns for park
            ss = trajfile.ReadLine().Split();
            park.x = Convert.ToDecimal(ss[0]);
            park.y = Convert.ToDecimal(ss[1]);

            // read to the last vertex element
            for (int i = 0; i < vertexes - 2; i++)
                trajfile.ReadLine();

            // store as dock
            ss = trajfile.ReadLine().Split();
            dock.x = Convert.ToDecimal(ss[0]);
            dock.y = Convert.ToDecimal(ss[1]);

            // convert offset to meters, convert park/dock to global meter coordinates
            globaloffset.x = globaloffset.x / 20;
            globaloffset.y = globaloffset.y / 20;

            park.x += globaloffset.x;
            park.y += globaloffset.y;
            dock.x += globaloffset.x;
            dock.y += globaloffset.y;
        }

        public string getTempPath()
        {
            return this.tempPath;
        }

        public string generatePGM() // Generates 2 .pgm files from originally selected image and returns the path to a new pgm with the same name as the png
        {
            // Set the temporary pgm file path
            this.tempPath = this.dirPath + "\\" + "temp.pgm";

            // Using ImageMagick.NET, convert the .png image to .pgm
            using (MagickImage pngMap = new MagickImage(this.imgPath))
            {
                pngMap.Write(this.tempPath);
                return this.tempPath;
            }
        }

        public string getStoreInfo(string info)
        {
            // Get the directory name
            string mapFolder = Path.GetFileName(this.dirPath);

            // Initialize return variable
            string storeInfo = "";

            // Get either the store name or the store number
            if (info == "name")
            {
                storeInfo = getStoreName(mapFolder);
            }
            else if (info == "number")
            {
                storeInfo = getStoreNumber(mapFolder);
            }
            return storeInfo;
        }

        public string getSaveFile()
        {
            SaveFileDialog saveFD = new SaveFileDialog();

            saveFD.Title = "Save the image file";
            saveFD.Filter = "pgm files (*.pgm)|*.pgm";
            //saveFD.FileName = "This is a test filename"; // This is how you set the filename
            saveFD.InitialDirectory = this.dirPath;

            DialogResult result = saveFD.ShowDialog();

            if (result == DialogResult.OK)
            {
                return saveFD.FileName;
            }
            else
            {
                return "err::no_map_selected";
            }
        }

        #endregion

        #region Private member functions

        private string getStoreName(string folder)
        {
            string storeName;

            // Assign the store name based on their acronym
            if (folder.Substring(0, 2).ToUpper() == "GM")
            {
                storeName = "Giant Martin";
            }
            else if (folder.Substring(0, 2).ToUpper() == "SS")
            {
                storeName = "Stop & Shop";
            }
            else if (folder.Length > 3)
            {
                if(folder.Substring(0, 3).ToUpper() == "SNS")
                    storeName = "Stop & Shop";
                else
                {
                    storeName = "Unknown Store";
                }
            }
            else
            {
                storeName = "Unknown Store";
            }
            return storeName;
        }

        private string getStoreNumber(string folder)
        {
            string storeNumber = "";

            // The only number in the directory name should be the store number
            for (int i = 0; i < folder.Length; i++)
            {
                if (Char.IsDigit(folder[i]))
                {
                    storeNumber += folder[i];
                }
            }
            return storeNumber;
        }

        private void loadFiles()
        {
            // Save all the files in this directory
            this.dirFiles = Directory.GetFiles(this.dirPath, "*.*", SearchOption.TopDirectoryOnly);
        }

        private string getFilebyType(string fileExt)
        {
            // Iterate through the directory files to find either the trajectory or offset file
            for (int i = 0; i < this.dirFiles.Length; i++)
            {
                if (fileExt == Path.GetExtension(this.dirFiles[i]))
                {
                    return this.dirFiles[i];
                }
            }
            // Should not be reached unless the file is missing
            return "";
        }

        #endregion
    }
}
