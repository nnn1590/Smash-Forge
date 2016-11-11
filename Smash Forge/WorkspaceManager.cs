﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using SALT.Scripting;
using SALT.Scripting.AnimCMD;
using SALT.Scripting.MSC;
using System.Xml;
using SALT.PARAMS;

namespace Smash_Forge
{
    public class WorkspaceManager
    {
        public WorkspaceManager(ProjectTree tree)
        {
            Projects = new List<Project>();
            Tree = tree;
        }

        public List<Project> Projects { get; set; }

        private ProjectTree Tree { get; set; }
        public string WorkspaceRoot { get; set; }
        public string TargetProject { get; set; }
        public string WorkspaceName { get; set; }

        public void OpenWorkspace(string filepath)
        {
            var wk = new XmlDocument();
            wk.Load(filepath);

            WorkspaceRoot = Path.GetDirectoryName(filepath);

            var rootNode = wk.SelectSingleNode("//Workspace");
            WorkspaceName = rootNode.Attributes["Name"].Value;
            var nodes = wk.SelectNodes("//Workspace//Project");
            foreach (XmlNode node in nodes)
            {
                var proj = ReadProjectFile(Path.Combine(WorkspaceRoot, node.Attributes["Path"].Value));
                proj.ProjName = node.Attributes["Name"].Value;
                Projects.Add(proj);
            }
            PopulateTreeView();
        }

        public void OpenProject(string filename)
        {
            Projects.Add(ReadProjectFile(filename));
            PopulateTreeView();
        }
        private Project ReadProjectFile(string filepath)
        {
            var proj = new Project();
            if (filepath.EndsWith(".fitproj", StringComparison.InvariantCultureIgnoreCase))
            {
                proj = new FitProj();
            }
            else if (filepath.EndsWith(".stproj", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new NotImplementedException("Stage projects not yet supported");
            }
            proj.ReadProject(filepath);
            proj.ProjName = Path.GetFileName(filepath);
            return proj;
        }

        public void PopulateTreeView()
        {
            Tree.treeView1.BeginUpdate();
            TreeNode workspaceNode = null;
            if (!string.IsNullOrEmpty(WorkspaceName))
            {
                workspaceNode = new TreeNode(WorkspaceName);
                workspaceNode.ImageIndex = workspaceNode.SelectedImageIndex = 2;
            }
            foreach (FitProj p in Projects)
            {
                FileInfo fileinfo = new FileInfo(p.ProjFilepath);
                var projNode = new ProjectNode(p);
                projNode.Tag = fileinfo;
                GetDirectories(new DirectoryInfo(p.ProjDirectory).GetDirectories(), projNode, p);
                GetFiles(new DirectoryInfo(p.ProjDirectory), projNode, p);
                if (workspaceNode != null)
                    workspaceNode.Nodes.Add(projNode);
                else
                    Tree.treeView1.Nodes.Add(projNode);
            }
            if (workspaceNode != null)
                Tree.treeView1.Nodes.Add(workspaceNode);

            Tree.treeView1.EndUpdate();
        }

        private void GetDirectories(DirectoryInfo[] subDirs, ProjectFolderNode nodeToAddTo, FitProj p)
        {
            ProjectFolderNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new ProjectFolderNode() { Text = subDir.Name };
                aNode.Tag = subDir;
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode, p);
                }
                GetFiles(subDir, aNode, p);
                nodeToAddTo.Nodes.Add(aNode);
            }
        }
        private void GetFiles(DirectoryInfo dir, ProjectFolderNode nodeToAddTo, FitProj p)
        {
            foreach (var fileinfo in dir.GetFiles())
            {
                if (fileinfo.Name.EndsWith(".fitproj", StringComparison.InvariantCultureIgnoreCase))
                    break;

                var child = new ProjectFileNode() { Text = fileinfo.Name };
                child.Tag = fileinfo;
                foreach (var f in p.IncludedFiles)
                    if (fileinfo.FullName.Contains(f.Path))
                    {
                        nodeToAddTo.Nodes.Add(child);
                        break;
                    }
            }
        }
    }

    public class Project
    {
        // Project Properties
        public XmlDocument ProjFile { get; set; }
        public string ProjFilepath { get; set; }
        public string ProjDirectory { get { return Path.GetDirectoryName(ProjFilepath); } }
        public string ProjName { get; set; }
        public string ToolVer { get; set; }
        public string GameVer { get; set; }
        public ProjType Type { get; set; }
        public ProjPlatform Platform { get; set; }
        public List<ProjectItem> IncludedFiles { get; set; }

        public virtual void ReadProject(string filepath) { }
    }

    public class FitProj : Project
    {
        public FitProj()
        {
            IncludedFiles = new List<ProjectItem>();
        }
        public FitProj(string name) : this()
        {
            ProjName = name;
        }
        public FitProj(string name, string filepath) : this(name)
        {
            ReadProject(filepath);
        }

        public override void ReadProject(string filepath)
        {
            ProjFilepath = filepath;
            var proj = new XmlDocument();
            proj.Load(filepath);

            var node = proj.SelectSingleNode("//Project");
            this.ToolVer = node.Attributes["ToolVer"].Value;
            this.GameVer = node.Attributes["GameVer"].Value;

            if (node.Attributes["Platform"].Value == "WiiU")
                this.Platform = ProjPlatform.WiiU;
            else if (node.Attributes["Platform"].Value == "3DS")
                this.Platform = ProjPlatform.ThreeDS;

            var nodes = proj.SelectNodes("//Project/FileGroup");
            foreach (XmlNode n in nodes)
            {
                foreach (XmlNode child in n.ChildNodes)
                {
                    var item = new ProjectItem();
                    item.Path = Runtime.CanonicalizePath(child.Attributes["include"].Value);
                    if (child.HasChildNodes)
                    {
                        foreach (XmlNode child2 in child.ChildNodes)
                        {
                            if (child2.LocalName == "DependsUpon")
                            {
                                var path = Runtime.CanonicalizePath(Path.Combine(Path.GetDirectoryName(item.Path), child2.InnerText));
                                item.Depends.Add(IncludedFiles.Find(x => x.Path == path));
                            }
                        }
                    }
                    IncludedFiles.Add(item);
                }
            }
            ProjFile = proj;
        }

        //public XmlDocument WriteFitproj(string filepath)
        //{
        //    var writer = XmlWriter.Create(filepath, new XmlWriterSettings() { Indent = true, IndentChars = "\t" });
        //    writer.WriteStartDocument();
        //    writer.WriteStartElement("Project");
        //    writer.WriteAttributeString("Name", ProjName);
        //    writer.WriteAttributeString("ToolVer", ToolVer);
        //    writer.WriteAttributeString("GameVer", GameVer);
        //    writer.WriteAttributeString("Platform", Enum.GetName(typeof(ProjPlatform), Platform));

        //    writer.WriteStartElement("MLIST");
        //    if (!string.IsNullOrEmpty(MLIST))
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", MLIST);
        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteStartElement("PARAMS");
        //    foreach (var param in PARAM_FILES)
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", param);
        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteStartElement("ACMD");
        //    foreach (var acmd in ACMD_FILES)
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", acmd);
        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteStartElement("MSC");
        //    foreach (var msc in MSC_FILES)
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", msc);
        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteStartElement("ANIM");
        //    foreach (var anim in ANIM_FILES)
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", anim);

        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteStartElement("MODEL");
        //    foreach (var mdl in MODEL_FILES)
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", mdl);
        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteStartElement("TEX");
        //    foreach (var tex in TEXTURE_FILES)
        //    {
        //        writer.WriteStartElement("Import");
        //        writer.WriteAttributeString("include", tex);
        //        writer.WriteEndElement();
        //    }
        //    writer.WriteEndElement();

        //    writer.WriteEndElement();
        //    writer.WriteEndDocument();
        //    writer.Close();
        //    var doc = new XmlDocument();
        //    doc.Load(filepath);
        //    return doc;
        //}
    }
    public class ProjectItem
    {
        public ProjectItem()
        {
            Depends = new List<ProjectItem>();
        }
        public string Path { get; set; }
        public List<ProjectItem> Depends { get; set; }
        public override string ToString()
        {
            return Path;
        }
    }
    public enum ProjType
    {
        Fighter,
        Stage
    }
    public enum ProjPlatform
    {
        WiiU,
        ThreeDS
    }
}
