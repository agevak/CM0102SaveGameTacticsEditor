using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CM.Save.Model;

namespace CM.Save
{
    public class CMSaveFile
    {
        private byte[] fileContent;
        private SaveReader saveReader;

        public CMSaveFile() { }

        public void Load(string filename)
        {
            if (filename == null) throw new ArgumentNullException("filename must be not null.");
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("filename must be not empty.");
            if (!File.Exists(filename)) throw new ArgumentException($"{filename} file doesn't exist.");
            fileContent = File.ReadAllBytes(filename);
            saveReader = new SaveReader();
            saveReader.Load(fileContent);
        }

        public void Save(string filename, bool? compressed = true)
        {
            if (filename == null) throw new ArgumentNullException("filename must be not null.");
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("filename must be not empty.");
            saveReader.Write(filename, compressed ?? saveReader.WasCompressed);
        }

        public IList<Tactic> ExtractAITactics()
        {
            IList<Tactic> result = new List<Tactic>();
            Block block = saveReader.FindBlock("tactics.dat");
            //File.WriteAllBytes("ExtractAITactics_ai.dat", block.dataBuffer);
            //saveReader.DumpBlockToFile("human_manager.dat", "ExtractAITactics_human.dat");
            using (BinaryReader br = new BinaryReader(new MemoryStream(block.dataBuffer)))
            {
                int baseOffset = 4, tactCountTotal = br.ReadInt32();
                for (int i = 0; i < Tactic.AI_PACK_FILENAMES.Length; i++, baseOffset += 0x18E3)
                {
                    if (i == 28) baseOffset += 4;
                    br.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
                    Tactic tactic = new Tactic() { Name = BytesToString(br.ReadBytes(51)) };
                    tactic.First250 = br.ReadBytes(250);
                    tactic.Next1115 = br.ReadBytes(1115);
                    br.BaseStream.Seek(baseOffset + 0x188B, SeekOrigin.Begin);
                    tactic.Next88 = br.ReadBytes(88);
                    br.BaseStream.Seek(baseOffset + 0x5B4, SeekOrigin.Begin);
                    tactic.Last11 = br.ReadBytes(11);
                    result.Add(tactic);
                    //File.WriteAllBytes($"_{tactic.Name}.tct", tactic.ToTctFile());
                }
            }
            return result;
        }

        public void ReplaceAITacticsWithSameOne(Tactic newTactic)
        {
            ReplaceAITactics(Enumerable.Repeat(newTactic, Tactic.AI_PACK_FILENAMES.Length).ToList());
        }

        public void ReplaceAITactics(IList<Tactic> newTactics)
        {
            if (newTactics == null) throw new ArgumentNullException("newTactics must be not null.");
            if (newTactics.Count != Tactic.AI_PACK_FILENAMES.Length) throw new ArgumentException($"newTactics must contain exactly {Tactic.AI_PACK_FILENAMES.Length} elements.");
            foreach (Tactic tactic in newTactics) if (tactic == null) throw new ArgumentException("newTactics may not contain null's.");
            Block block = saveReader.FindBlock("tactics.dat");
            using (BinaryWriter bw = new BinaryWriter(new MemoryStream(block.dataBuffer)))
            {
                int baseOffset = 4;
                for (int i = 0; i < Tactic.AI_PACK_FILENAMES.Length; i++, baseOffset += 0x18E3)
                {
                    if (i == 28) baseOffset += 4;
                    bw.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
                    newTactics[i].WriteToSavFile(bw);
                }
            }
            //File.WriteAllBytes("ReplaceAITactics.dat", block.dataBuffer);
        }

        public void ReplaceHumanTactic(Tactic newTactic)
        {
            Block block = saveReader.FindBlock("human_manager.dat");
            using (BinaryWriter bw = new BinaryWriter(new MemoryStream(block.dataBuffer)))
            {
                int baseOffset = 0x45D84;
                bw.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
                newTactic.WriteToSavFile(bw);
            }
            //File.WriteAllBytes("ReplaceHumanTactic.dat", block.dataBuffer);
        }

        private string BytesToString(byte[] bytes)
        {
            string s = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            if (s.Contains("\0")) s = s.Substring(0, s.IndexOf("\0"));
            return s;
        }
    }
}
