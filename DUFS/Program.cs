using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace DUFS
{

    class Program
    {
        string dir = "..\\..\\..\\FS\\";
        public static void Main(string[] args)
        {
            Program p = new Program();

            Console.WriteLine("Welcome to the Davenport University File System (DUFS)");

            while (true)
            {

                Console.WriteLine("Please enter a command:");
                string input = Console.ReadLine();
                string[] inputArgs = input.Split(" ");

                switch (inputArgs[0])
                {
                    case "allocate"://ALLOCATE <VOLUMENAME>, <SIZE>: Create a file system called VOLUMENAME that contains SIZE bytes.
                        p.allocate(inputArgs[1], Convert.ToInt32(inputArgs[2]));
                        break;
                    case "deallocate"://DEALLOCATE <VOLUMENAME>: Physically deletes the volume called VOLUMENAME.
                        p.deallocate(inputArgs[1]);
                        break;
                    case "truncate"://TRUNCATE <VOLUMENAME>: Initializes (erases) the volume called VOLUMENAME. 
                        p.truncate(inputArgs[1]);
                        break;
                    case "dump"://DUMP <VOLUMENAME>: Displays the contents of the volume called VOLUMENAME on the screen. 
                        p.dump(inputArgs[1]);
                        break;
                    case "mount":
                        Console.WriteLine($"Mounting {inputArgs[1]}...");
                        p.volume(p, inputArgs[1]);
                        break;
                    case "info"://Display size of specified volume.
                        p.volInfo(inputArgs[1]);
                        break;
                }
            }


        }

        public void volume(Program p, string inputVolume)
        {
            string path = dir + inputVolume;
            try
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                stream.Close();
            }
            catch 
            {
                Console.WriteLine("Volume not found! Please mount a exisiting volume!");
                return;
            }
            
            Console.WriteLine("Volume Mounted!");
            bool endFlag = false;

            while (endFlag == false)
            {
                Console.WriteLine($"Please enter command for {inputVolume}:");
                string input2 = Console.ReadLine();
                string[] inputArgs2 = input2.Split(" ");
                switch (inputArgs2[0])
                {
                    case "catalog"://Display files in the current mounted volume.
                        p.catalog(inputVolume);
                        break;
                    case "unmount"://Unmount current volume.
                        endFlag = true;
                        break;
                    case "info"://display size of specified file.
                        p.info(inputVolume, inputArgs2[1]);
                        break;
                    case "create"://Create new, empty file with specified name and size.
                        p.create(inputVolume, inputArgs2[1], Convert.ToInt32(inputArgs2[2]));
                        break;
                    case "write"://Write data to specified filename starting at specified offset.
                        p.write(inputVolume, inputArgs2[1], Convert.ToInt32(inputArgs2[2]), inputArgs2[3]);
                        break;
                    case "read"://Reads specified file from specified start to end.
                        Console.WriteLine("Output from file: " + p.read(inputVolume, inputArgs2[1], Convert.ToInt32(inputArgs2[2]), Convert.ToInt32(inputArgs2[3])));
                        break;
                    case "delete"://Removes specified file from volume ONLY IF READONLY IS FALSE!
                        p.delete(inputVolume, inputArgs2[1]);
                        break;
                    case "truncate"://Same as DELETE <FILENAME> followed by CREATE < FILENAME >
                        p.truncate(inputVolume, inputArgs2[1]);
                        break;
                    case "set"://SET <FILENAME> READOLNY=TRUE|FALSE: Sets the read-only flag for FILENAME.
                        p.set(inputVolume, inputArgs2[1], inputArgs2[2]);
                        break;


                }
            }
        }

        public void allocate(string name, int size)
        {
            Console.WriteLine($"Allocating {size} bytes for '{name}'...");
            string path = dir + name;
            File.WriteAllBytes(@path, new byte[size]);
        }

        public void deallocate(string name)
        {
            Console.WriteLine($"Deallocating '{name}'...");
            string path = dir + name;
            File.Delete(@path);
        }
        public void truncate(string name)
        {
            Console.WriteLine($"Truncating '{name}'...");
            string path = dir + name;
            byte[] content;
            try
            {
                content = File.ReadAllBytes(@path);
            }
            catch
            {
                Console.WriteLine("VOLUME NOT FOUND");
                return;
            }
            int size = content.Length;
            File.WriteAllBytes(path, new byte[size]);
            Console.WriteLine($"Done!");
        }
        public void dump(string name)
        {
            string path = dir + name;
            try
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                stream.Close();
            }
            catch
            {
                Console.WriteLine("Volume not found! Please mount a exisiting volume!");
                return;
            }
            Console.WriteLine($"Dumping '{name}'...");
            
            byte[] content;
            try
            {
                content = File.ReadAllBytes(@path);
            }
            catch
            {
                Console.WriteLine("VOLUME NOT FOUND");
                return;
            }
            foreach (byte b in content)
            {
                Console.Write("0x{0:x2}", b);
                Console.Write(" ");

            }
            Console.Write("\n");
        }
        public void create(string volName, string fileName, int fileSize)
        {
            
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            var fi = new FileInfo(path);
            long volumeSize = fi.Length;
            int projectedSize = fileName.Length + fileSize.ToString().Length + 46 + fileSize;
            Console.WriteLine($"Projected File Size: {projectedSize}");
            

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                for (int i = 0; i < content.Length; i++)
                {
                    if (i + projectedSize > volumeSize)
                    {
                        Console.WriteLine("PROJECTED FILE SIZE LARGER THAN VOLUME OR NOT ENOUGH SPACE IN VOLUME! ABORTING CREATION");
                        return;
                    }
                start:
                    stream.Position = i;
                    if (stream.ReadByte() != 1 && flag == false)
                    {
                        
                        for (int j = 0; j < projectedSize - 1; j++)
                        {
                            if (stream.ReadByte() != 0)
                            {
                                i = Convert.ToInt16(stream.Position);
                                Console.WriteLine("Not enough space!");
                                goto start;
                            }
                        }
                        stream.Position = i;

                        break;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

                stream.WriteByte(1); //Mark beginning of file.

                //------------------------------------------------------
                //-------------WRITE FILE NAME--------------------------
                //------------------------------------------------------
                foreach (char c in fileName)
                {
                    stream.WriteByte((byte)c);

                }

                stream.WriteByte(2);//Mark end of name section and start of the properties section.

                //------------------------------------------------------
                //-------------WRITE FILE SIZE--------------------------
                //------------------------------------------------------
                foreach (char c in fileSize.ToString())
                {
                    stream.WriteByte(((byte)c));
                }


                stream.WriteByte(1);
                

                //------------------------------------------------------
                //-------------WRITE FILE CREATE DATE-------------------
                //------------------------------------------------------
                string date = DateTime.Now.Month.ToString();
                if (date.Length < 2)//Fix date length to xx/xx/xxxx
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Day.ToString();
                if (date.Length < 2)//Fix date lenght to xx/xx/xxxx
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Year.ToString();
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }

                stream.WriteByte(1);

                //------------------------------------------------------
                //-------------WRITE FILE CREATE Time-------------------
                //------------------------------------------------------
                string time = DateTime.Now.Hour.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Minute.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Second.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte(1);
                //------------------------------------------------------
                //-------------WRITE FILE LAST MODIFIED DATE-------------------
                //------------------------------------------------------
                date = DateTime.Now.Month.ToString();
                if (date.Length < 2)
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Day.ToString();
                if (date.Length < 2)
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Year.ToString();
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }

                stream.WriteByte(1);

                //------------------------------------------------------
                //-------------WRITE FILE LAST MODIFIED Time-------------------
                //------------------------------------------------------
                time = DateTime.Now.Hour.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Minute.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Second.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte(1);


                stream.WriteByte(4);//Mark as NOT readonly by default.

                stream.WriteByte(3);//Mark end of property section AND beginning of data section.
                while (fileSize > 0)
                {
                    fileSize--;
                    stream.WriteByte(0);
                }
                stream.WriteByte(6);//Mark end of file.
                stream.Close();
            }
        }

        public void write(string volName, string fileName, int offset, string fileData)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {

                for (int i = 0; i < content.Length; i++)
                {
                start:
                    if (i > content.Length)
                    {
                        return;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 1 || flag == false)
                    {
                        desiredFilePos = i;
                        foreach (char c in fileName)
                        {
                            if ((byte)c == stream.ReadByte())
                            {
                                continue;
                            }
                            else
                            {
                                //desiredFilePos = 0;
                                i++;
                                goto start;
                            }
                        }
                        if (stream.ReadByte() != 2)
                        {
                            i++;
                            goto start;
                        }
                        stream.Position--;
                        goto label;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

            label:
                while (stream.ReadByte() != 2)
                { }
                while (stream.ReadByte() != 1) { }
                while (stream.ReadByte() != 1) { }
                while (stream.ReadByte() != 1) { }

                //------------------------------------------------------
                //-------------WRITE FILE LAST MODIFIED DATE-------------------
                //------------------------------------------------------
                string date = DateTime.Now.Month.ToString();
                if (date.Length < 2)
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Day.ToString();
                if (date.Length < 2)
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Year.ToString();
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }

                while (stream.ReadByte() != 1) { }

                //------------------------------------------------------
                //-------------WRITE FILE LAST MODIFIED Time-------------------
                //------------------------------------------------------
                string time = DateTime.Now.Hour.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Minute.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Second.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }



                while (stream.ReadByte() != 3)
                { }
                stream.Position += offset;
                foreach (char c in fileData)
                {
                    if (stream.ReadByte() != 6)
                    {
                        stream.Position--;
                        stream.WriteByte((byte)c);
                    }
                    else
                    {
                        Console.WriteLine("FILE SPACE NOT LARGE ENOUGH TO WRITE DATA TO! DATA HAS BEEN CROPPED TO FIT!");
                        break;
                    }
                }
                stream.Close();
                return;
                
            }
        }

        public string read(string volName, string fileName, int offsetStart, int offsetEnd)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {

                for (int i = 0; i < content.Length; i++)
                {
                start:
                    if (i > content.Length)
                    {
                        return "";
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 1 || flag == false)
                    {
                        desiredFilePos = i;
                        foreach (char c in fileName)
                        {
                            if ((byte)c == stream.ReadByte())
                            {
                                continue;
                            }
                            else
                            {
                                //desiredFilePos = 0;
                                i++;
                                goto start;
                            }
                        }
                        if (stream.ReadByte() != 2)
                        {
                            i++;
                            goto start;
                        }
                        stream.Position--;
                        goto label;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

            label:
                while (stream.ReadByte() != 3) { }
                string output = "";
                stream.Position += offsetStart;
                for (int i = 0; i < (offsetEnd - offsetStart); i++)
                {
                    if (stream.ReadByte() == 6)
                    {
                        break;
                    }
                    else 
                    {
                        stream.Position--;
                        output = output + (char)stream.ReadByte();
                    }
                    
                }
                stream.Close();
                return output;
            }
        }

        public string info(string volName, string fileName)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {

                for (int i = 0; i < content.Length; i++)
                {
                start:
                    if (i > content.Length)
                    {
                        return "";
                    }
                    stream.Position = i;
                    desiredFilePos = i;
                    if (stream.ReadByte() == 1 || flag == false)
                    {
                        
                        foreach (char c in fileName)
                        {
                            if ((byte)c == stream.ReadByte())
                            {
                                continue;
                            }
                            else
                            {
                                //desiredFilePos = 0;
                                i++;
                                goto start;
                            }
                        }
                        if (stream.ReadByte() != 2)
                        {
                            i++;
                            goto start;
                        }
                        stream.Position--;
                        goto label;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

            label:
                string output = "";
                stream.Position = desiredFilePos + 1;
                while (stream.ReadByte() != 2)
                {
                    stream.Position--;
                    output += (char)stream.ReadByte();
                }
                Console.WriteLine($"File Name: '{output}'");
                output = "";

                //---File Size---
                while (stream.ReadByte() != 1)
                {
                    stream.Position--;
                    output += (char)stream.ReadByte();
                }
                Console.WriteLine($"File Size: {output} Bytes");
                output = "";
                //---Date Created---
                while (stream.ReadByte() != 1)
                {
                    stream.Position--;
                    output += (char)stream.ReadByte();
                }
                Console.WriteLine($"Date Created: {output}");
                output = "";

                //---Time Created---
                while (stream.ReadByte() != 1)
                {
                    stream.Position--;
                    output += (char)stream.ReadByte();
                }
                Console.WriteLine($"Time Created: {output}");
                output = "";

                //---Date Last Edited---
                while (stream.ReadByte() != 1)
                {
                    stream.Position--;
                    output += (char)stream.ReadByte();
                }
                Console.WriteLine($"Date Last Edited: {output}");
                output = "";

                //---Time Last Edited---
                while (stream.ReadByte() != 1)
                {
                    stream.Position--;
                    output += (char)stream.ReadByte();
                }
                Console.WriteLine($"Time Last Edited: {output}");
                output = "";

                string readOnly ="";
                if (stream.ReadByte() == 4)
                {
                    readOnly = "false";
                }
                else 
                {
                    readOnly = "true";
                }
                Console.WriteLine($"Read-Only: {readOnly}");

                stream.Close();
                return output;
            }
        }

        public void set(string volName, string fileName, string fileRead)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {

                for (int i = 0; i < content.Length; i++)
                {
                start:
                    if (i > content.Length)
                    {
                        return;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 1 || flag == false)
                    {
                        desiredFilePos = i;
                        foreach (char c in fileName)
                        {
                            if ((byte)c == stream.ReadByte())
                            {
                                continue;
                            }
                            else
                            {
                                //desiredFilePos = 0;
                                i++;
                                goto start;
                            }
                        }
                        if (stream.ReadByte() != 2)
                        {
                            i++;
                            goto start;
                        }
                        stream.Position--;
                        goto label;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

            label:
                while (stream.ReadByte() != 3) { }
                stream.Position -= 2;
                if (fileRead == "readonly=true")
                {
                    stream.WriteByte(5);
                }
                else if (fileRead == "readonly=false")
                {
                    stream.WriteByte(4);
                }
                else
                {
                    Console.WriteLine("ERROR IN INPUT");
                    return;
                }
                Console.WriteLine($"Read-Only Flag Set for {fileName}!");
                stream.Close();
            }
        }

        public void delete(string volName, string fileName)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {

                for (int i = 0; i < content.Length; i++)
                {
                start:
                    if (i > content.Length)
                    {
                        return;
                    }
                    stream.Position = i;
                    desiredFilePos = i;
                    if (stream.ReadByte() == 1 || flag == false)
                    {

                        foreach (char c in fileName)
                        {
                            if ((byte)c == stream.ReadByte())
                            {
                                continue;
                            }
                            else
                            {
                                //desiredFilePos = 0;
                                i++;
                                goto start;
                            }
                        }
                        if (stream.ReadByte() != 2)
                        {
                            i++;
                            goto start;
                        }
                        stream.Position--;
                        goto label;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

            label:
                while (stream.ReadByte() != 3)
                { }
                stream.Position -= 2;

                if (stream.ReadByte() == 4)
                {
                    while (stream.ReadByte() != 2)//Back up to beginning of file.
                    {
                        stream.Position -= 2;
                    }
                    while (stream.ReadByte() != 1)//Back up to beginning of file.
                    {
                        stream.Position -= 2;
                    }
                    stream.Position--;
                    while (stream.ReadByte() != 6)
                    {
                        stream.Position--;
                        stream.WriteByte(0);
                    }
                    stream.Position--;
                    stream.WriteByte(0);
                    stream.Close();
                }
                else
                {
                    Console.WriteLine("FILE IS SET READONLY! CANNOT DELETE!");
                    stream.Close();
                    return;
                }
                
            }
        }


        public void truncate(string volName, string fileName)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {

                for (int i = 0; i < content.Length; i++)
                {
                start:
                    if (i > content.Length)
                    {
                        return;
                    }
                    stream.Position = i;
                    desiredFilePos = i;
                    if (stream.ReadByte() == 1 || flag == false)
                    {

                        foreach (char c in fileName)
                        {
                            if ((byte)c == stream.ReadByte())
                            {
                                continue;
                            }
                            else
                            {
                                //desiredFilePos = 0;
                                i++;
                                goto start;
                            }
                        }
                        if (stream.ReadByte() != 2)
                        {
                            i++;
                            goto start;
                        }
                        stream.Position--;
                        goto label;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }
                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }
                }

            label:
                while (stream.ReadByte() != 2)//Back up to size field
                {
                   
                }
                

                string strFileSize = "";

                while (stream.ReadByte() != 1)
                {
                    stream.Position--;
                    strFileSize += (char)stream.ReadByte();
                }
                
                

                int fileSize = Convert.ToInt32(strFileSize);

                while (stream.ReadByte() != 1)//Back up to beginning of file.
                {
                    stream.Position -= 2;
                }
                //stream.Position--; Keep the 1 at the beginning!
                while (stream.ReadByte() != 6)
                {
                    stream.Position--;
                    stream.WriteByte(0);
                }
                



                stream.Position = desiredFilePos + 1;

                //------------------------------------------------------
                //-------------WRITE FILE NAME--------------------------
                //------------------------------------------------------
                foreach (char c in fileName)
                {
                    stream.WriteByte((byte)c);

                }

                stream.WriteByte(2);//Mark end of name section and start of the properties section.

                //------------------------------------------------------
                //-------------WRITE FILE SIZE--------------------------
                //------------------------------------------------------
                foreach (char c in fileSize.ToString())
                {
                    stream.WriteByte(((byte)c));
                }


                stream.WriteByte(1);


                //------------------------------------------------------
                //-------------WRITE FILE CREATE DATE-------------------
                //------------------------------------------------------
                string date = DateTime.Now.Month.ToString();
                if (date.Length < 2)//Fix date lenght to xx/xx/xxxx
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Day.ToString();
                if (date.Length < 2)//Fix date lenght to xx/xx/xxxx
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Year.ToString();
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }

                stream.WriteByte(1);

                //------------------------------------------------------
                //-------------WRITE FILE CREATE Time-------------------
                //------------------------------------------------------
                string time = DateTime.Now.Hour.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Minute.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Second.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte(1);
                //------------------------------------------------------
                //-------------WRITE FILE LAST MODIFIED DATE-------------------
                //------------------------------------------------------
                date = DateTime.Now.Month.ToString();
                if (date.Length < 2)
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Day.ToString();
                if (date.Length < 2)
                {
                    date = "0" + date;
                }
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)'/');
                date = DateTime.Now.Year.ToString();
                foreach (char c in date)
                {
                    stream.WriteByte(((byte)c));
                }

                stream.WriteByte(1);

                //------------------------------------------------------
                //-------------WRITE FILE LAST MODIFIED Time-------------------
                //------------------------------------------------------
                time = DateTime.Now.Hour.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Minute.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte((byte)':');
                time = DateTime.Now.Second.ToString();
                if (time.Length < 2)//fix time format to xx:xx:xx
                {
                    time = "0" + time;
                }
                foreach (char c in time)
                {
                    stream.WriteByte(((byte)c));
                }
                stream.WriteByte(1);


                stream.WriteByte(4);//Mark as NOT readonly by default.

                stream.WriteByte(3);//Mark end of property section AND beginning of data section.
                while (fileSize > 0)
                {
                    fileSize--;
                    stream.WriteByte(0);
                }
                stream.WriteByte(6);//Mark end of file.
                stream.Close();
            }
        }


        public void catalog(string volName)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int desiredFilePos = 0;

            Console.WriteLine($"Files in {volName}:");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                for (int i = 0; i < content.Length; i++)
                {
                    stream.Position = i;
                    if (stream.ReadByte() == 1 && flag == false)
                    {
                        while (stream.ReadByte() != 2)
                        {
                            stream.Position--;
                            Console.Write((char)stream.ReadByte());
                        }
                        Console.Write("\n");
                    }

                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                    }

                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                    }

                }
            }
        }

        public void volInfo(string volName)
        {
            string path = dir + volName;
            byte[] content;
            content = File.ReadAllBytes(@path);
            bool flag = false;
            int freeSpace = 0;
            int fileCount = 0;

            var fi = new FileInfo(path);
            long volSize = fi.Length;
            Console.WriteLine($"Volume Size: {volSize} Bytes");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                for (int i = 0; i < content.Length; i++)
                {
                    stream.Position = i;
                    if (stream.ReadByte() == 1 && flag == false)
                    {
                        fileCount++;
                        continue;
                    }

                    stream.Position = i;
                    if (stream.ReadByte() == 0 && flag == false)
                    {
                        freeSpace++;
                        continue;
                    }

                    stream.Position = i;
                    if (stream.ReadByte() == 6)
                    {
                        flag = false;
                        continue;
                    }

                    stream.Position = i;
                    if (stream.ReadByte() != 1)
                    {
                        flag = true;
                        continue;
                    }

                    
                }
                stream.Close();
            }

            Console.WriteLine($"Free Space Available in '{volName}': {freeSpace} Bytes");
            Console.WriteLine($"Number of files in '{volName}': {fileCount}");
        }
    }

}
