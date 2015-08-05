using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using xxHashSharp;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Threading.Tasks;

namespace PatchGen
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public delegate void NextFile(string  file);
        public event NextFile onNextFile;
        Dictionary<string, FileStateInfo> GeneratePatch(string path, Dictionary<string, FileStateInfo> report)
        {
            Dictionary<string, FileStateInfo> patchreport = new Dictionary<string, FileStateInfo>();
            var dir = GetFileStates(path);
            onProgress += Form1_onProgress;
            onNextFile += Form1_onNextFile;
            double max = dir.Count;
            double cur = 0;
            foreach (var fs in dir)
            {
                fs.Value.onFileScanProgress += Value_onFileScanProgress;
                onNextFile(fs.Value.shortpath);
                if (report.ContainsKey(fs.Value.shortpath))
	                {
		                fs.Value.CompareFile (report[fs.Value.shortpath]);
                    }
                else
                {
                    fs.Value.ScanFile();
                }
                if (fs.Value.chunks.Count>0)
                {
                    patchreport.Add(fs.Value.shortpath,fs.Value );
                }
                fs.Value.onFileScanProgress -= Value_onFileScanProgress;
                cur += 1.0;
                onProgress(cur / max);
            }
            return patchreport;
        }

        DateTime lastfile = DateTime.Now;
        void Form1_onNextFile(string file)
        {
            var time = DateTime.Now;
            var ts = time - lastfile;
            if (ts.Milliseconds>500)
            {
                lastfile = time;
                if (label1.InvokeRequired)
                {
                    label1.BeginInvoke(new MethodInvoker(delegate
                    {
                        label1.Text = file;
                    }));
                }
                else
                {
                    label1.Text = file;
                    this.Invalidate();
                    label1.Invalidate();
                    Application.DoEvents();

                }
            }
            
        }

        void Value_onFileScanProgress(double  value)
        {
            var time = DateTime.Now;
            var ts = time - lastfile;
            if (ts.Milliseconds > 500)
            {
                lastfile = time;
                value = Math.Ceiling(value);
                if (progressBar1.InvokeRequired)
                {
                    progressBar1.BeginInvoke(new MethodInvoker(delegate
                    {
                        progressBar1.Value = (int)value;
                    }));
                }
                else
                {
                    progressBar1.Value = (int)value;
                    progressBar1.Invalidate();
                    Application.DoEvents();
                }
            }
            
        }

        public delegate void Progress(double value);
        public event Progress onProgress;
        public delegate void FilePatchProgress(double value);
        public event FilePatchProgress onFileScanProgress;
        public void RepPatchProgress(int num, int chunkcount,int len,int chunksize)
        {
            if (len > chunksize * 150)
            {
                double d = (double)num / (double)chunkcount;
                d = d * 100;
                d = Math.Ceiling(d);
                int dd = (int)d;
                if (d % 5 == 0)
                {
                    if (onFileScanProgress != null)
                    {
                        onFileScanProgress((int)d);
                    }
                }
            }
        }
        void Apply(string path, string patchfile)
        {
            var patch = LoadReport(patchfile);
            onProgress += Form1_onProgress;
            onNextFile += Form1_onNextFile;
            double max = patch.Count ;
            double cur = 0;
            foreach (var item in patch)
            {
                onNextFile(item.Value.shortpath);
                using (FileStream fs = File.Open(path + item.Value.shortpath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.SetLength(item.Value.len);
                    int max2 = item.Value.chunks.Count ;
                    int cur2 = 0;
                    int len;
                    if (item.Value.len>int.MaxValue )
                    {
                        len = int.MaxValue - 1;
                    }
                    else
                    {
                        len = (int)item.Value.len;
                    }
                    foreach (var chunk in item.Value.chunks)
                    {
                        if (chunk.Value.data != null)
                        { 
                            //fs.Seek(chunk.Value.startposition, SeekOrigin.Begin);
                            fs.Seek(0, SeekOrigin.Begin);
                            for (int i = 0; i < chunk.Value.num; i++)
                            {
                                fs.Seek(500000, SeekOrigin.Current);
                            }
                            fs.Write(chunk.Value.data, 0, chunk.Value.data.Length);
                            cur2 ++;
                            RepPatchProgress(cur2, max2, len, item.Value.chunksize);
                        }
                        else
                        {
                            MessageBox.Show("ERROR DATA IS NULL");
                        }
                    }
                }
                cur += 1.0;
                onProgress(cur / max);
            }
        }
        Dictionary<string, FileStateInfo> GenerateReport(string path)
        {
            var dir = GetFileStates(path);
            onProgress += Form1_onProgress;
            onNextFile += Form1_onNextFile;
            onNextFile += Form1_onNextFile;
            onNextFile += Form1_onNextFile;
            onNextFile += Form1_onNextFile;
            double max = dir.Count;
            double cur = 0; 
            foreach (var fs in dir)
            {
                fs.Value.onFileScanProgress += Value_onFileScanProgress;
                onNextFile(fs.Value.shortpath );
                fs.Value.ScanFile();
                fs.Value.onFileScanProgress -= Value_onFileScanProgress;
                cur += 1.0;
                onProgress(cur / max);
            }
            return dir;
        }

        void Form1_onProgress(double value)
        {
            value = value * 100;
            value = Math.Ceiling(value);
            if (progressBar2.InvokeRequired)
            {
                progressBar2.BeginInvoke(new MethodInvoker(delegate
                {
                    progressBar2.Value = (int)value;
                }));
            }
            else
            {
                progressBar2.Value = (int)value;
            }
        }

        Dictionary<string, FileStateInfo> GetFileStates(string path)
        {
            Dictionary<string, FileStateInfo> dir = new Dictionary<string, FileStateInfo>();
            var allfiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories );
            foreach (var file in allfiles)
            {
                FileStateInfo fs = new FileStateInfo();
                fs.path = file;
                fs.shortpath = file.Replace(path, "");
                dir.Add(fs.shortpath, fs);
            };
            return dir;
        }





        void GetCommandLine()
        {
            string[] argsraw = Environment.GetCommandLineArgs();
            // Command line parsing
            List<string> args = new List<string>();
            foreach (var item in argsraw)
            {

                if (item.Replace(".vshost", "").ToLower() != Application.ExecutablePath.ToLower())
                {
                    args.Add(item);
                }
            }
            var param = new Dictionary<string, string>();
            foreach (var item in argsraw)
            {
                param.Add(item, item.Replace("-", ""));
            }
            Arguments CommandLine = new Arguments(args.ToArray());
            // Look for specific arguments values and display 
            // them if they exist (return null if they don't)
           

            //Console.WriteLine("  -bc блочное копирование папки с проверкой содержимого");
            bool createreport = param.ContainsValue("cr") ? true : false;
            bool createpatch = param.ContainsValue("cp") ? true : false;
            bool applypatch = param.ContainsValue("ap") ? true : false;
            bool blockcopy = param.ContainsValue("bc") ? true : false;
            if (createreport || createpatch || applypatch || blockcopy)
            {
                comline = true;
                if (createreport)
                {
                    Console.WriteLine("  -cr создание отчета о состоянии папки (конечной папки)");
                    if (CommandLine["target"] == null)
                    {
                        Console.WriteLine("     -target путь к конечной папке");
                        return;
                    }
                    if (CommandLine["report"] == null)
                    {
                        Console.WriteLine("     -report=\"путь\" путь к новому файлу отчета");
                        return;
                    }
                    CreateReport(CommandLine["target"], CommandLine["report"]);
                }
                else if (createpatch)
                {
                    Console.WriteLine("  -cp создание патча на основе отчета и исходной папки");
                    if (CommandLine["report"] == null)
                    {
                        Console.WriteLine("     -report=\"путь\" путь к новому файлу отчета");
                        return;
                    }
                    if (CommandLine["patch"] == null)
                    {
                        Console.WriteLine("     -patch=\"путь\" путь к новому файлу патча");
                        return;
                    }
                    if (CommandLine["source"] == null)
                    {
                        Console.WriteLine("     -source=\"путь\" путь к искодной папке");
                        return;
                    }
                    CreatePatch(CommandLine["source"], CommandLine["report"], CommandLine["patch"]);                }
                else if (applypatch)
                {
                    Console.WriteLine("  -ap применение патча к конечной папке");
                    if (CommandLine["target"] == null)
                    {
                        Console.WriteLine("     -target путь к конечной папке");
                        return;
                    }
                    if (CommandLine["patch"] == null)
                    {
                        Console.WriteLine("     -patch=\"путь\" путь к новому файлу патча");
                        return;
                    }
                    ApplyPatch(CommandLine["target"], CommandLine["patch"]);
                }
            }
            else
            {
                Console.WriteLine("Возможные параметры:");
                Console.WriteLine("  -cr создание отчета о состоянии папки (конечной папки)");
                Console.WriteLine("     -target путь к конечной папке");
                Console.WriteLine("     -report=\"путь\" путь к новому файлу отчета");

                Console.WriteLine("  -cp создание патча на основе отчета и исходной папки");
                Console.WriteLine("     -source=\"путь\" путь к искодной папке");
                Console.WriteLine("     -report=\"путь\" путь к файлу отчета");
                Console.WriteLine("     -patch=\"путь\" путь к новому файлу патча");

                Console.WriteLine("  -ap применение патча к конечной папке");
                Console.WriteLine("     -target путь к конечной папке");
                Console.WriteLine("     -patch=\"путь\" путь к файлу патча");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
           
        }

        bool comline = false;
        void CreateReport(string target,string reportpatch)
        {
            MethodInvoker mi = new MethodInvoker(delegate
            {
                ShowAnimation();
                var report = GenerateReport(target);
                Save(report, reportpatch);
                HideAnimation();
                
            });
            mi.BeginInvoke(null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog flr = new FolderBrowserDialog();
            if (flr.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = flr.SelectedPath;
                SaveFileDialog sfd = new SaveFileDialog();
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CreateReport(path, sfd.FileName);
                }
            }
        }

        void ShowAnimation()
        {
            if (panel1.InvokeRequired )
            {
                panel1.BeginInvoke(new MethodInvoker(delegate
                {
                    this.ControlBox = false;
                    panel1.Visible = true;
                    panel1.Width = 255;
                    panel1.Height = 147;
                }));
            }
            else
            {
                this.ControlBox = false;
                panel1.Visible = true;
                panel1.Width = 247;
                panel1.Height = 147;
            }
        }

        void HideAnimation()
        {
            if (panel1.InvokeRequired)
            {
                panel1.BeginInvoke(new MethodInvoker(delegate
                {
                    this.ControlBox = true ;
                    panel1.Visible = false;
                }));
            }
            else
            {
                this.ControlBox = true ;
                panel1.Visible = false;
            }
            if (comline)
            {
                Application.Exit();
            }
        }


        public void Save(Dictionary<string, FileStateInfo> data, string file)
        {
            //Сохраняем резервную копию
            BinaryFormatter bf = new BinaryFormatter();
            //откроем поток для записи в файл
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (GZipStream gz = new GZipStream(fs, CompressionMode.Compress, false))
            {
                bf.Serialize(gz, data);//сериализация
            }
        }
        public Dictionary<string, FileStateInfo> LoadReport(string file)
        {
            Dictionary<string, FileStateInfo> report;
            if (!File.Exists(file))
            {
                report  = new Dictionary<string, FileStateInfo>(); //указать тип нового объекта
                return report;
            }
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress, false))
            {
                report = (Dictionary<string, FileStateInfo>)bf.Deserialize(gz); //указать тип объекта
            }
            return report;
        }

        void CreatePatch(string sourcedir, string reportfile, string patchfile)
        {
            MethodInvoker mi = new MethodInvoker(delegate
            {
                ShowAnimation();
                var lrep = LoadReport(reportfile);
                var patch = GeneratePatch(sourcedir, lrep);
                Save(patch, patchfile);
                HideAnimation();
            });
            mi.BeginInvoke(null, null);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog flr = new FolderBrowserDialog();
            if (flr.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = flr.SelectedPath;
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Выберите файл отчета";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        CreatePatch(flr.SelectedPath, ofd.FileName, sfd.FileName);
                    }
                }
            }
        }

        void ApplyPatch(string target,string patch)
        {
            MethodInvoker mi = new MethodInvoker(delegate
            {
                ShowAnimation();
                Apply(target, patch);
                HideAnimation();
            });
            mi.BeginInvoke(null, null);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog flr = new FolderBrowserDialog();
            if (flr.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = flr.SelectedPath;
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Выберите файл отчета";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ApplyPatch(flr.SelectedPath, ofd.FileName);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выберите файл отчета";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OpenFileDialog ofd2 = new OpenFileDialog();
                ofd2.Title = "Выберите файл отчета";
                if (ofd2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MethodInvoker mi = new MethodInvoker(delegate
                    {
                        SaveFileDialog sfd = new SaveFileDialog();
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            ShowAnimation();
                            var lrep = LoadReport(ofd.FileName);
                            var rrep = LoadReport(ofd2.FileName);
                            foreach (var item in rrep)
                            {
                                if (lrep.ContainsKey(item.Key))
                                {
                                    foreach (var chu in item.Value.chunks)
                                    {
                                        if (lrep[item.Key].chunks.ContainsKey(chu.Key))
                                        {
                                            lrep[item.Key].chunks.Remove(chu.Key);
                                            lrep[item.Key].chunks.Add(chu.Key, chu.Value);
                                        }
                                        else
                                        {
                                            lrep[item.Key].chunks.Add(chu.Key, chu.Value);
                                        }
                                    }
                                }
                                else
                                {
                                    lrep.Add(item.Key, item.Value);
                                }
                            }
                            Save(lrep, sfd.FileName);
                            HideAnimation();
                        }
                    });
                    mi.BeginInvoke(null, null);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Tick -= timer1_Tick;
            GetCommandLine();
        }
    }
}
