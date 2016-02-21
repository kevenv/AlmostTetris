using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace Tetris
{
    public struct Actions
    {
        public int moveX;
        public int moveY;
        public bool rotate;
        public bool drop;
    };

    class ReplayEntry
    {
        public long ticks { get; private set; }
        public string action { get; private set; }

        public ReplayEntry(long ticks, string action)
        {
            if(string.IsNullOrEmpty(action)) {
                throw new TetrisException("ReplayEntry: No action");
            }
            this.ticks = ticks;
            this.action = action;
        }

        public override string ToString()
        {
            return ticks + "  " + action;
        }
    };

    class ReplaySave
    {
        private List<int> blocsID;
        private List<ReplayEntry> actions;

        public int blocsIDCount {
            get { return blocsID.Count; }
            private set { }
        }
        public int actionsCount {
            get { return actions.Count; }
            private set { }
        }

        private string fileNamePath;
        private string fileName;
        private string fileExtension;
        public string fileNameFullPath
        {
            get { return fileNamePath + "\\" + fileName + "." + fileExtension; }
            private set { }
        }

        public int currentReplayEntry { get; private set; }
        public int currentBlocID { get; private set; }
        public bool replayFinish { get; private set; }
        public int replaySpeed { get; private set; }

        public ReplaySave(string fileNamePath)
        {
            //todo: check file path OK
            //parseFileNamePath(fileNamePath);
            this.fileName = fileNamePath;
            replaySpeed = 1;
            blocsID = new List<int>();
            actions = new List<ReplayEntry>();
            init();
        }

        public void init()
        {
            currentBlocID = 0;
            currentReplayEntry = 0;
            replayFinish = false;
        }

        private void parseFileNamePath(string fileNamePath)
        {
            int lastDot = fileNamePath.LastIndexOf('.');
            if(lastDot == -1) {
                lastDot = fileNamePath.Length;
            }

            int lastSlash = fileNamePath.LastIndexOf('\\');
            if(lastSlash == -1) {
                lastSlash = lastDot;
            }

            this.fileNamePath = fileNamePath.Substring(0, fileNamePath.Length - lastSlash);
            this.fileName = fileNamePath.Substring(lastSlash, fileNamePath.Length - lastDot);
            this.fileExtension = fileNamePath.Substring(lastDot, fileNamePath.Length);
        }

        public void saveReplayFileBin()
        {
            FileStream fs = new FileStream(fileName + ".raw",FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write((ushort)blocsIDCount);

            foreach (int blocId in blocsID) {
                bw.Write((byte)blocId);
            }

            foreach (ReplayEntry entry in actions) {
                bw.Write((uint)entry.ticks);
                bw.Write((byte)actionToByte(entry.action));
            }

            fs.Close();
            bw.Close();

            compressReplayFile();
        }

        public void loadReplayBin(string fileName)
        {
            System.Console.WriteLine("* Loading replay '" + fileName + "'");

            clear();

            decompressReplayFile(fileName);

            FileStream fs = new FileStream(fileName + ".raw", FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            //read blocsIDCount (16 bits)
            int blocsIDCount = (int)br.ReadUInt16();

            //read blocs (8 bits)
            for (int i = 0; i < blocsIDCount; i++) {
                blocsID.Add((int)br.ReadByte());
            }
            
            //read actions
            int start = 2 + 1 * blocsIDCount;
            for (int i = start; i < fs.Length; i+=4+1) {
                actions.Add(new ReplayEntry( (long)br.ReadUInt32(), //32 bits
                                             byteToAction(br.ReadByte())));      //8 bits
            }

            fs.Close();
            br.Close();
            File.Delete(fileName + ".raw");
        }

        private void compressReplayFile()
        {
            FileStream uncompressedFile = new FileStream(fileName + ".raw", FileMode.Open);
            FileStream compressedFile = File.Create(fileName);

            GZipStream zipper = new GZipStream(compressedFile, CompressionMode.Compress);
            uncompressedFile.CopyTo(zipper);

            zipper.Close();
            compressedFile.Close();
            uncompressedFile.Close();
            File.Delete(fileName + ".raw");
        }

        private void decompressReplayFile(string fileName)
        {
            FileStream compressedFile = new FileStream(fileName, FileMode.Open);
            FileStream uncompressedFile = File.Create(fileName + ".raw");

            GZipStream zipper = new GZipStream(compressedFile, CompressionMode.Decompress);
            zipper.CopyTo(uncompressedFile);

            zipper.Close();
            uncompressedFile.Close();
            compressedFile.Close();
        }

        private byte actionToByte(string action)
        {
            if (action == "left") {
                return 0;
            }
            else if (action == "right") {
                return 1;
            }
            else if (action == "down") {
                return 2;
            }
            else if (action == "rotate") {
                return 3;
            }
            else if (action == "drop") {
                return 4;
            }
            else {
                return 7; //no action
            }
        }

        private string byteToAction(byte data)
        {
            switch (data) {
            case 0:
                return "left";
            case 1:
                return "right";
            case 2:
                return "down";
            case 3:
                return "rotate";
            case 4:
                return "drop";
            case 7:
            default:
                throw new TetrisException("ReplaySave: corrupt replay save");
            }
        }

        public void saveReplayFile()
        {
            System.Console.WriteLine("* Saving replay to '" + fileName + "'");

            using (StreamWriter sw = new StreamWriter(fileName)) {
                saveNbBlocs(sw);
                saveBlocsID(sw);
                saveActions(sw);
            }
        }

        private void saveNbBlocs(StreamWriter sw)
        {
            sw.WriteLine(blocsID.Count);
        }

        private void saveBlocsID(StreamWriter sw)
        {
            foreach (int blocId in blocsID) {
                sw.WriteLine(blocId);
            }
        }

        private void saveActions(StreamWriter sw)
        {
            foreach (ReplayEntry entry in actions) {
                sw.WriteLine(entry.ticks);
                sw.WriteLine(entry.action);
            }
        }

        public void loadReplay(string fileName)
        {
            System.Console.WriteLine("* Loading replay '" + fileName + "'");

            clear();

            using (StreamReader sr = new StreamReader(fileName)) {
                int nbBlocs = readNbBlocs(sr);
                readBlocsID(sr, nbBlocs);
                readActions(sr);
            }
        }

        private int readNbBlocs(StreamReader sr)
        {
            return int.Parse(sr.ReadLine());
        }

        private void readBlocsID(StreamReader sr, int nbBlocs)
        {
            for (int i = 0; i < nbBlocs; i++) {
                int id = int.Parse(sr.ReadLine());
                blocsID.Add(id);
            }
        }

        private void readActions(StreamReader sr)
        {
            while (sr.Peek() >= 0) {
                actions.Add(new ReplayEntry(long.Parse(sr.ReadLine()), sr.ReadLine()));
            }
        }

        public void loadNextInputs(long ticks, ref Actions actions)
        {
            if (currentReplayEntry != actionsCount) {
                ReplayEntry entry = this.actions[currentReplayEntry];
                if (ticks >= speedTicks(entry.ticks)) {
                    System.Console.WriteLine("* Replay action: " + entry.action + " " + "(" + entry.ticks + ")");
                    handleAction(entry.action, ref actions);
                    currentReplayEntry++;
                }
            }
            else {
                System.Console.WriteLine("* Replay finish");
                currentReplayEntry = 0;
                replayFinish = true;
            }
        }

        private long speedTicks(long ticks)
        {
            if (replaySpeed < 0) {
                return ticks * -replaySpeed; //slower
            }
            else {
                return ticks / replaySpeed; //faster
            }
        }

        public void handleAction(string action, ref Actions actions)
        {
            if (action == "left") {
                actions.moveX = -1;
            }
            else if (action == "right") {
                actions.moveX = 1;
            }
            else if (action == "rotate") {
                actions.rotate = true;
            }
            else if (action == "down") {
                actions.moveY = 1;
            }
            else if (action == "drop") {
                actions.drop = true;
            }
        }

        public void clear()
        {
            blocsID.Clear();
            actions.Clear();
        }

        public ReplayEntry getReplayEntry(int index)
        {
            return actions[index];
        }

        public void addEntry(long ticks, string action)
        {
            actions.Add(new ReplayEntry(ticks, action));
        }

        public int getBlocID(int index)
        {
            return blocsID[index];
        }

        public int getNextBlocID()
        {
            currentBlocID++;
            return blocsID[currentBlocID - 1];
        }

        public void addBlocID(int id)
        {
            blocsID.Add(id);
        }

        public void setReplaySpeed(int value)
        {
            if (value == 0) {
                replaySpeed = 1;
            }
            else {
                replaySpeed = value;
            }
        }

        public override string ToString()
        {
            string output = "";
            output += "BlocsID: " + blocsIDCount + "\n";
            foreach(int id in blocsID) {
                output += id + ",";
            }

            output += "\nActions: " + actionsCount + "\n";
            foreach (ReplayEntry entry in actions) {
                output += entry + "\n";
            }

            return output;
        }
    }
}
