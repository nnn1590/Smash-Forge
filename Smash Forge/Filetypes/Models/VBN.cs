﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace SmashForge
{
    public class Bone : TreeNode
    {
        public VBN vbnParent;
        public UInt32 boneType;
        public UInt32 boneRotationType;
        public bool IsInverted = true;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (value)
                {
                    foreach (Bone b in vbnParent.bones)
                    {
                        b.Selected = false;
                    }
                    _selected = true;
                }
                else
                    _selected = value;
            }
        }
        private bool _selected = false;

        public enum BoneType
        {
            Normal = 0,
            Follow,
            Helper,
            Swing
        }

        public enum BoneRotationType
        {
            Euler,
            Quaternion,
        }


        public UInt32 boneId;
        public float[] position = new float[] { 0, 0, 0 };
        public float[] rotation = new float[] { 0, 0, 0 };
        public float[] scale = new float[] { 1, 1, 1 };

        public Vector3 pos = Vector3.Zero, sca = new Vector3(1f, 1f, 1f);
        public Quaternion rot = Quaternion.FromMatrix(Matrix3.Zero);
        public Matrix4 transform, invert;

        public bool isSwingBone = false;

        public int parentIndex
        {
            set
            {
                if (Parent != null) Parent.Nodes.Remove(this);
                if (value > -1 && value < vbnParent.bones.Count)
                {
                    vbnParent.bones[value].Nodes.Add(this);
                }
            }

            get
            {
                if (Parent == null)
                    return -1;
                return vbnParent.bones.IndexOf((Bone)Parent);
            }
        }

        public Bone(VBN v)
        {
            vbnParent = v;
            ImageKey = "bone";
            SelectedImageKey = "bone";
        }

        public List<Bone> GetChildren()
        {
            List<Bone> l = new List<Bone>();
            foreach (Bone b in vbnParent.bones)
                if (b.Parent == this)
                    l.Add(b);
            return l;
        }

        public override string ToString()
        {
            return Text;
        }

        public static float rot90 = (float)(90 * (Math.PI / 180));

        public int CheckControl(Rendering.Ray r)
        {
            /*
            Vector3 pos_c = Vector3.Transform(Vector3.Zero, transform);
            if (RenderTools.intersectCircle(pos_c, 2f, 30, r.p1, r.p2))
                return 1;
           
            */
            return -1;
        }

        public void Draw()
        {
            Vector3 pos_c = Vector3.TransformPosition(Vector3.Zero, transform);
            // first calcuate the point and draw a point
            if (IsSelected || Selected)
            {
                /*GL.Color3(Color.Red);
                RenderTools.drawCircleOutline(pos_c, 2f, 30, Matrix4.CreateRotationX(0));
                GL.Color3(Color.Green);
                RenderTools.drawCircleOutline(pos_c, 2f, 30, Matrix4.CreateRotationX(rot90));
                GL.Color3(Color.Gold);
                RenderTools.drawCircleOutline(pos_c, 2f, 30, Matrix4.CreateRotationY(rot90));*/
                GL.Color3(Color.Red);
            }
            else
                GL.Color3(Color.GreenYellow);

            Rendering.ShapeDrawing.DrawCube(pos_c, Runtime.renderBoneNodeSize);

            // now draw line between parent
            GL.Color3(Color.LightBlue);
            GL.LineWidth(2f);

            GL.Begin(PrimitiveType.Lines);
            if (Parent != null && Parent is Bone)
            {
                Vector3 pos_p = Vector3.TransformPosition(Vector3.Zero, ((Bone)Parent).transform);
                GL.Vertex3(pos_c);
                GL.Color3(Color.Blue);
                GL.Vertex3(pos_p);
            }
            GL.End();
        }
    }

    public class HelperBone
    {
        public void Read(FileData f)
        {
            f.endian = Endianness.Little;
            f.Seek(4);
            int count = f.ReadInt();
            f.Skip(12);
            int dataCount = f.ReadInt();
            int boneCount = f.ReadInt();
            int hashCount = f.ReadInt();
            int hashOffset = f.ReadInt() + 0x28;
            f.Skip(4);

            int pos = f.Pos();
            f.Seek(hashOffset);

            csvHashes csv = new csvHashes(Path.Combine(MainForm.executableDir, "hashTable.csv"));
            List<string> bonename = new List<string>();

            for (int i = 0; i < hashCount; i++)
            {
                uint hash = (uint)f.ReadInt();
                Console.WriteLine(csv.ids[hash]);
                bonename.Add(csv.ids[hash]);
            }

            f.Seek(pos);
            Console.WriteLine("Count " + count);

            for (int i = 0; i < dataCount; i++)
            {
                Console.WriteLine("Bone " + i + " start at " + f.Pos().ToString("x"));
                // 3 sections
                int secLength = f.ReadInt();
                int someCount = f.ReadInt(); // usually 2?

                for (int sec = 0; sec < 5; sec++)
                {
                    int size = f.ReadInt();
                    int id = f.ReadInt();
                    Console.WriteLine(id + ":\t" + size.ToString("x"));
                    for (int j = 0; j < ((size - 1) / 4) - 1; j++)
                    {

                        if (id == 4)
                        {
                            short b1 = f.ReadShort();
                            short b2 = f.ReadShort();
                            Console.Write("\t" + (b1 == -1 ? b1 + "" : bonename[b1]) + " " + b2 + "\t");
                        }
                        else
                        if (id == 5)
                        {
                            short b1 = f.ReadShort();
                            short b2 = f.ReadShort();
                            Console.Write("\t" + (b1 == -1 ? b1 + "" : bonename[b1]) + " " + (b2 == -1 ? b2 + "" : bonename[b2]) + "\t");
                        }
                        else
                            Console.Write("\t" + (f.ReadUShort() / (id == 7 ? (float)0xffff : 1)) + " " + (f.ReadUShort() / (id == 7 ? (float)0xffff : 1)) + "\t");
                    }
                    Console.WriteLine();
                }

                f.Skip(8);
            }

            Console.WriteLine("0x" + f.Pos().ToString("X"));
            f.Skip(8);
            int hashSize = f.ReadInt();
            int unk = f.ReadInt();



        }
    }

    public class VBN : FileBase
    {
        public VBN()
        {
            Text = "model.vbn";
            ImageKey = "skeleton";
            SelectedImageKey = "skeleton";

            ContextMenu = new ContextMenu();

            MenuItem OpenEdit = new MenuItem("Open Editor");
            OpenEdit.Click += OpenEditor;
            ContextMenu.MenuItems.Add(OpenEdit);

            MenuItem save = new MenuItem("Save As");
            ContextMenu.MenuItems.Add(save);
            save.Click += Save;


            MenuItem swingMAX = new MenuItem("Preview Swing MAX");
            ContextMenu.MenuItems.Add(swingMAX);
            swingMAX.Click += SetSwingMax;

            MenuItem swingMID = new MenuItem("Preview Swing MID");
            ContextMenu.MenuItems.Add(swingMID);
            swingMID.Click += SetSwingMid;

            MenuItem swingMIN = new MenuItem("Preview Swing MIN");
            ContextMenu.MenuItems.Add(swingMIN);
            swingMIN.Click += SetSwingMin;


            ResetNodes();
        }

        public VBN(string filename) : this()
        {
            FilePath = filename;
            Read(filename);
        }

        public override Endianness Endian { get; set; }

        public string FilePath = "";
        public Int16 unk_1 = 2, unk_2 = 1;
        public UInt32 totalBoneCount;
        public UInt32[] boneCountPerType = new UInt32[4];
        public List<Bone> bones = new List<Bone>();

        public SB SwingBones
        {
            get
            {
                if (_swingBones == null)
                    _swingBones = new SB();
                return _swingBones;
            }
            set
            {
                _swingBones = value;
                ResetNodes();
            }
        }
        private SB _swingBones;
        public JTB JointTable
        {
            get
            {
                if (_jointTable == null)
                    _jointTable = new JTB();
                return _jointTable;
            }
            set
            {
                _jointTable = value;
                ResetNodes();
            }
        }
        private JTB _jointTable;

        private TreeNode RootNode = new TreeNode() { Text = "Bones" };

        #region Events

        public BoneTreePanel Editor;

        private void OpenEditor(object sender, EventArgs args)
        {
            RootNode.Nodes.Clear();
            if (Editor == null || Editor.IsDisposed)
            {
                Editor = new BoneTreePanel(this);
                Editor.FilePath = FilePath;
                Editor.Text = Parent.Text + "/" + Text;
                MainForm.Instance.AddDockedControl(Editor);
            }
            else
            {
                Editor.BringToFront();
            }
        }

        public void ResetNodes()
        {
            Nodes.Clear();

            Nodes.Add(RootNode);
            Nodes.Add(SwingBones);
        }


        public void SetSwingMax(object sender, EventArgs args)
        {

            float ToRad = (float)Math.PI / 180;
            if (_swingBones != null)
            {
                foreach (SB.SBEntry sb in _swingBones.bones)
                {
                    foreach (Bone b in bones)
                    {
                        if (b.boneId == sb.hash)
                        {
                            b.rot = FromEulerAngles(b.rotation[2], b.rotation[1], b.rotation[0]) *
                                FromEulerAngles(sb.rz2 * ToRad, sb.ry2 * ToRad, 0);
                            break;
                        }
                    }
                }
            }
            update(false);
        }
        public void SetSwingMin(object sender, EventArgs args)
        {
            float ToRad = (float)Math.PI / 180;
            if (_swingBones != null)
            {
                foreach (SB.SBEntry sb in _swingBones.bones)
                {
                    foreach (Bone b in bones)
                    {
                        if (b.boneId == sb.hash)
                        {
                            b.rot = FromEulerAngles(b.rotation[2], b.rotation[1], b.rotation[0]) *
                                FromEulerAngles(sb.rz1 * ToRad, sb.ry1 * ToRad, 0);
                        }
                    }
                }
            }
            update(false);
        }
        public void SetSwingMid(object sender, EventArgs args)
        {
            float ToRad = (float)Math.PI / 180;
            if (_swingBones != null)
            {
                foreach (SB.SBEntry sb in _swingBones.bones)
                {
                    foreach (Bone b in bones)
                    {
                        if (b.boneId == sb.hash)
                        {
                            b.rot = FromEulerAngles(b.rotation[2], b.rotation[1], b.rotation[0]) *
                                FromEulerAngles((sb.rz1 + sb.rz2) / 2 * ToRad, (sb.ry1 + sb.ry2) / 2 * ToRad, 0);
                        }
                    }
                }
            }
            update(false);
        }

        public void Save(object sender, EventArgs args)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Visual Bones Namco (.vbn)|*.vbn|" +
                             "All Files (*.*)|*.*";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, Rebuild());
                }
            }
        }

        #endregion

        public Bone getBone(String name)
        {
            foreach (Bone bo in bones)
                if (bo.Text.Equals(name))
                    return bo;
            return null;
        }
        public Bone getBone(uint hash)
        {
            foreach (Bone bo in bones)
                if (bo.boneId == hash)
                    return bo;
            return null;
        }

        public List<Bone> getBoneTreeOrderol()
        {
            List<Bone> bone = new List<Bone>();
            Queue<Bone> q = new Queue<Bone>();

            q.Enqueue(bones[0]);

            while (q.Count > 0)
            {
                Bone b = q.Dequeue();
                foreach (Bone bo in b.GetChildren())
                    q.Enqueue(bo);
                bone.Add(b);
            }
            return bone;
        }

        public List<Bone> getBoneTreeOrder()
        {
            if (bones.Count == 0)
                return null;
            List<Bone> bone = new List<Bone>();
            Queue<Bone> q = new Queue<Bone>();

            queueBones(bones[0], q);

            while (q.Count > 0)
            {
                bone.Add(q.Dequeue());
            }
            return bone;
        }

        public void queueBones(Bone b, Queue<Bone> q)
        {
            q.Enqueue(b);
            foreach (Bone c in b.GetChildren())
                queueBones(c, q);
        }

        
        public static Quaternion FromQuaternionAngles(float z, float y, float x, float w)
        {
            {
                Quaternion q = new Quaternion();
                q.X = x;
                q.Y = y;
                q.Z = z;
                q.W = w;
                
                if (q.W < 0)
                    q *= -1;

                //return xRotation * yRotation * zRotation;
                return q;
            }
        }

        public static Quaternion FromEulerAngles(float z, float y, float x)
        {
            {
                Quaternion xRotation = Quaternion.FromAxisAngle(Vector3.UnitX, x);
                Quaternion yRotation = Quaternion.FromAxisAngle(Vector3.UnitY, y);
                Quaternion zRotation = Quaternion.FromAxisAngle(Vector3.UnitZ, z);

                Quaternion q = (zRotation * yRotation * xRotation);

                if (q.W < 0)
                    q *= -1;

                //return xRotation * yRotation * zRotation;
                return q;
            }
        }

        private bool Updated = false;
        public void update(bool reset = false)
        {
            Updated = true;
            List<Bone> nodesToProcess = new List<Bone>();
            // Add all root nodes from the VBN
            foreach (Bone b in bones)
                if (b.Parent == null)
                    nodesToProcess.Add(b);

            // some special processing for the root bones before we start
            foreach (Bone b in nodesToProcess)
            {
                b.transform = Matrix4.CreateScale(b.sca) * Matrix4.CreateFromQuaternion(b.rot) * Matrix4.CreateTranslation(b.pos);
                // scale down the model in its entirety only when mid-animation (i.e. reset == false)
                if (!reset && Runtime.modelScale != 1) b.transform *= Matrix4.CreateScale(Runtime.modelScale);
            }

            // Process as a tree from the root node's children and beyond. These
            // all use the same processing, unlike the root nodes.
            int numRootNodes = nodesToProcess.Count;
            for (int i = 0; i < numRootNodes; i++)
            {
                nodesToProcess.AddRange(nodesToProcess[0].GetChildren());
                nodesToProcess.RemoveAt(0);
            }
            while (nodesToProcess.Count > 0)
            {
                // DFS
                Bone currentBone = nodesToProcess[0];
                nodesToProcess.RemoveAt(0);
                nodesToProcess.AddRange(currentBone.GetChildren());

                // Process this node
                currentBone.transform = Matrix4.CreateScale(currentBone.sca) * Matrix4.CreateFromQuaternion(currentBone.rot) * Matrix4.CreateTranslation(currentBone.pos);
                if (currentBone.Parent != null)
                {
                    currentBone.transform = currentBone.transform * ((Bone)currentBone.Parent).transform;
                }
            }
        }

        //public void updateOld(bool reset = false)
        //{
        //    for (int i = 0; i < bones.Count; i++)
        //    {
        //        bones[i].transform = Matrix4.CreateScale(bones[i].sca) * Matrix4.CreateFromQuaternion(bones[i].rot) * Matrix4.CreateTranslation(bones[i].pos);

        //        // Scale down the model only when in animations (e.g. reset == false)
        //        if (i == 0 && !reset && Runtime.model_scale != 1) bones[i].transform *= Matrix4.CreateScale(Runtime.model_scale);

        //        if (bones[i].Parent !=null)
        //        {
        //            bones[i].transform = bones[i].transform * bones[(int)bones[i].parentIndex].transform;
        //        }
        //    }
        //}

        public void reset(bool Main = true)
        {
            //if(Main)
            {
                /*RootNode.Nodes.Clear();
                if (bones.Count > 0 && bones[0].Parent == null)
                    RootNode.Nodes.Add(bones[0]);*/
            }

            ExpandAll();
            for (int i = 0; i < bones.Count; i++)
            {
                bones[i].pos = new Vector3(bones[i].position[0], bones[i].position[1], bones[i].position[2]);

                if (bones[i].boneRotationType == 1)
                {
                    bones[i].rot = (FromQuaternionAngles(bones[i].rotation[2], bones[i].rotation[1], bones[i].rotation[0], bones[i].rotation[3]));
                }
                else
                {
                    bones[i].rot = (FromEulerAngles(bones[i].rotation[2], bones[i].rotation[1], bones[i].rotation[0]));
                }
                bones[i].sca = new Vector3(bones[i].scale[0], bones[i].scale[1], bones[i].scale[2]);
            }
            update(true);
            for (int i = 0; i < bones.Count; i++)
            {
                try
                {
                    bones[i].invert = Matrix4.Invert(bones[i].transform);
                }
                catch (InvalidOperationException)
                {
                    bones[i].invert = Matrix4.Zero;
                }
            }
            if (Runtime.modelScale != 1f) update();
        }

        public override void Read(string filename)
        {
            FileData file = new FileData(filename);
            if (file != null)
            {
                file.endian = Endianness.Little;
                Endian = Endianness.Little;
                string magic = file.ReadString(0, 4);
                if (magic == "VBN ")
                {
                    file.endian = Endianness.Big;
                    Endian = Endianness.Big;
                }

                file.Seek(4);

                unk_1 = file.ReadShort();
                unk_2 = file.ReadShort();
                totalBoneCount = (UInt32)file.ReadInt();
                boneCountPerType[0] = (UInt32)file.ReadInt();
                boneCountPerType[1] = (UInt32)file.ReadInt();
                boneCountPerType[2] = (UInt32)file.ReadInt();
                boneCountPerType[3] = (UInt32)file.ReadInt();

                int[] pi = new int[totalBoneCount];
                for (int i = 0; i < totalBoneCount; i++)
                {
                    Bone temp = new Bone(this);
                    temp.Text = file.ReadString(file.Pos(), -1);
                    file.Skip(64);
                    temp.boneType = (UInt32)file.ReadInt();
                    pi[i] = file.ReadInt();
                    temp.boneId = (UInt32)file.ReadInt();
                    temp.position = new float[3];
                    temp.rotation = new float[3];
                    temp.scale = new float[3];
                    //temp.isSwingBone = temp.Text.Contains("__swing");
                    bones.Add(temp);
                }

                for (int i = 0; i < bones.Count; i++)
                {
                    bones[i].position[0] = file.ReadFloat();
                    bones[i].position[1] = file.ReadFloat();
                    bones[i].position[2] = file.ReadFloat();
                    bones[i].rotation[0] = file.ReadFloat();
                    bones[i].rotation[1] = file.ReadFloat();
                    bones[i].rotation[2] = file.ReadFloat();
                    bones[i].scale[0] = file.ReadFloat();
                    bones[i].scale[1] = file.ReadFloat();
                    bones[i].scale[2] = file.ReadFloat();
                    Bone temp = bones[i];
                    temp.parentIndex = pi[i];
                    //Debug.Write(temp.parentIndex);
                    //if (temp.parentIndex != 0x0FFFFFFF && temp.parentIndex > -1)
                    //    bones[temp.parentIndex].children.Add(i);
                    bones[i] = temp;
                }
                reset();
            }
        }

        public override byte[] Rebuild()
        {
            FileOutput file = new FileOutput();
            if (file != null)
            {
                if (Endian == Endianness.Little)
                {
                    file.endian = Endianness.Little;
                    file.WriteString(" NBV");
                    file.WriteShort(0x02);
                    file.WriteShort(0x01);
                }
                else if (Endian == Endianness.Big)
                {
                    file.endian = Endianness.Big;
                    file.WriteString("VBN ");
                    file.WriteShort(0x01);
                    file.WriteShort(0x02);
                }


                file.WriteInt(bones.Count);
                if (boneCountPerType[0] == 0)
                    boneCountPerType[0] = (uint)bones.Count;

                List<Bone> Normal = new List<Bone>();
                List<Bone> Unk = new List<Bone>();
                List<Bone> Helper = new List<Bone>();
                List<Bone> Swing = new List<Bone>();

                string[] SpecialBones = new string[] { "TransN",
"RotN",
"HipN",
"LLegJ",
"LKneeJ",
"LFootJ",
"LToeN",
"RLegJ",
"RKneeJ",
"RFootJ",
"RToeN",
"WaistN",
"BustN",
"LShoulderN",
"LShoulderJ",
"LArmJ",
"LHandN",
"RShoulderN",
"RShoulderJ",
"RArmJ",
"RHandN",
"NeckN",
"HeadN",
"RHaveN",
"LHaveN",
"ThrowN"};
                Bone[] Special = new Bone[SpecialBones.Length];
                int specialCount = 0;
                // OrderPass
                foreach (Bone b in bones)
                {
                    for (int i = 0; i < SpecialBones.Length; i++)
                    {
                        if (b.Text.Equals(SpecialBones[i]) || (SpecialBones[i].Equals("RotN") && b.Text.Equals("XRotN")))
                        {
                            specialCount++;
                            Special[i] = b;
                            break;
                        }
                    }
                }
                Console.WriteLine(SpecialBones.Length + " " + specialCount);
                if (specialCount == SpecialBones.Length)
                    Normal.AddRange(Special);

                //Gather Each Bone Type
                foreach (Bone b in bones)
                {
                    switch (b.boneType)
                    {
                        case 0: if (!Normal.Contains(b)) Normal.Add(b); break;
                        case 2: Helper.Add(b); break;
                        case 3: Swing.Add(b); break;
                        default: Unk.Add(b); break;
                    }
                }

                file.WriteInt(Normal.Count);
                file.WriteInt(Unk.Count);
                file.WriteInt(Helper.Count);
                file.WriteInt(Swing.Count);

                List<Bone> NewBoneOrder = new List<Bone>();
                NewBoneOrder.AddRange(Normal);
                NewBoneOrder.AddRange(Unk);
                NewBoneOrder.AddRange(Helper);
                NewBoneOrder.AddRange(Swing);
                bones.Clear();
                bones = NewBoneOrder;

                for (int i = 0; i < bones.Count; i++)
                {
                    file.WriteString(bones[i].Text);
                    for (int j = 0; j < 64 - bones[i].Text.Length; j++)
                        file.WriteByte(0);
                    file.WriteInt((int)bones[i].boneType);
                    if (bones[i].parentIndex == -1)
                        file.WriteInt(0x0FFFFFFF);
                    else
                        file.WriteInt(bones[i].parentIndex);
                    file.WriteInt((int)bones[i].boneId);
                }

                for (int i = 0; i < bones.Count; i++)
                {
                    file.WriteFloat(bones[i].position[0]);
                    file.WriteFloat(bones[i].position[1]);
                    file.WriteFloat(bones[i].position[2]);
                    file.WriteFloat(bones[i].rotation[0]);
                    file.WriteFloat(bones[i].rotation[1]);
                    file.WriteFloat(bones[i].rotation[2]);
                    file.WriteFloat(bones[i].scale[0]);
                    file.WriteFloat(bones[i].scale[1]);
                    file.WriteFloat(bones[i].scale[2]);
                }
            }
            return file.GetBytes();
        }

        /*public void readJointTable(string fname)
        {
            FileData d = new FileData(fname);
            d.Endian = Endianness.Big;
 
            int tableSize = 2;
 
            int table1 = d.readShort();
 
            if (table1 * 2 + 2 >= d.size())
                tableSize = 1;
 
            int table2 = -1;
            if (tableSize != 1)
                table2 = d.readShort();
 
            //if (table2 == 0)
            //    d.seek(d.pos() - 2);
 
            List<int> t1 = new List<int>();
 
            for (int i = 0; i < table1; i++)
                t1.Add(d.readShort());
 
            jointTable.Clear();
            jointTable.Add(t1);
 
            if (tableSize != 1)
            {
                List<int> t2 = new List<int>();
                for (int i = 0; i < table2; i++)
                    t2.Add(d.readShort());
                jointTable.Add(t2);
            }
        }*/

        public Bone bone(string name)
        {
            foreach (Bone b in bones)
            {
                if (b.Text.Equals(name))
                {
                    return b;
                }
            }
            throw new Exception("No bone of char[] name");
        }

        public int boneIndex(string name)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Text.Equals(name))
                {
                    return i;
                }
            }

            return -1;
            //throw new Exception("No bone of char[] name");
        }

        public int getJTBIndex(string name)
        {
            int index = -1;
            int vbnIndex = boneIndex(name);
            if (JointTable != null)
            {
                for (int i = 0; i < JointTable.Tables.Count; i++)
                {
                    for (int j = 0; j < JointTable.Tables[i].Count; j++)
                    {
                        if (JointTable.Tables[i][j] == vbnIndex)
                        {
                            // Note that some bones appear twice in the joint tables
                            // and this function will only find the first occurrence.
                            index = j + (i * 1000);
                            return index;
                        }
                    }
                }
            }
            return index;
        }

        public void deleteBone(int index)
        {
            boneCountPerType[bones[index].boneType]--;
            totalBoneCount--;
            List<Bone> children = bones[index].GetChildren();
            bones.RemoveAt(index);
            foreach (Bone b in children)
                deleteBone(bones.IndexOf(b));
        }

        public void deleteBone(string name)
        {
            deleteBone(boneIndex(name));
        }

        public float[] f = null;
        public Matrix4[] bonemat = { };
        public Matrix4[] bonematIT = { };
        public Matrix4[] bonematNoInv = { };

        public Matrix4[] GetShaderMatrices()
        {
            if (Updated)
            {
                Updated = false;
                if (bonemat.Length != bones.Count)
                {
                    bonemat = new Matrix4[bones.Count];
                    bonematNoInv = new Matrix4[bones.Count];
                }

                for (int i = 0; i < bones.Count; i++)
                {
                     bonemat[i] = bones[i].invert * bones[i].transform;
                    bonematNoInv[i] = bones[i].transform;
                }
            }

            return bonemat;
        }

        public Matrix4[] GetShaderMatricesNoInverse()
        {
            return bonematNoInv;
        }

        private static string charsToString(char[] c)
        {
            string boneNameRigging = "";
            foreach (char b in c)
                if (b != (char)0)
                    boneNameRigging += b;
            return boneNameRigging;
        }

        public static string BoneNameFromHash(uint boneHash)
        {
            /*foreach (ModelContainer m in Runtime.ModelContainers)
                if (m.VBN != null)
                    foreach (Bone b in m.VBN.bones)
                        if (b.boneId == boneHash)
                            return b.Text;*/

            /*csvHashes csv = new csvHashes(Path.Combine(MainForm.executableDir, "hashTable.csv"));
            for (int i = 0; i < csv.ids.Count; i++)
                if (csv.ids[i] == boneHash)
                    return csv.names[i]+" (From hashTable.csv)";*/

            return $"[Bonehash {boneHash.ToString("X")}]";
        }

        public Bone GetBone(uint boneHash)
        {
            if (boneHash == 3449071621)
                return null;
            foreach (Bone b in bones)
                if (b.boneId == boneHash)
                    return b;
            //MessageBox.Show("Open the VBN before editing the SB");
            return null;
        }

        public bool essentialComparison(VBN compareTo)
        {
            // Because I don't want to override == just for a cursory bone comparison
            if (this.bones.Count != compareTo.bones.Count)
                return false;

            for (int i = 0; i < this.bones.Count; i++)
            {
                if (this.bones[i].Name != compareTo.bones[i].Name)
                    return false;
                if (this.bones[i].pos != compareTo.bones[i].pos)
                    return false;
            }
            return true;
        }
    }

    public class SB : FileBase
    {
        public override Endianness Endian
        {
            get;
            set;
        }

        public class SBEntry
        {
            public uint hash = 3449071621;
            public float param1_1, param2_1, param2_2;
            public int param1_2, param1_3, param2_3;
            public float rx1, rx2, ry1, ry2, rz1, rz2;
            public uint[] boneHashes = new uint[8] { 3449071621, 3449071621, 3449071621, 3449071621, 3449071621, 3449071621, 3449071621, 3449071621 };
            public float[] unks1 = new float[4], unks2 = new float[6];
            public float factor;
            public int[] ints = new int[3];

            public override string ToString()
            {
                return VBN.BoneNameFromHash(hash);
            }
        }

        public string FilePath;
        public List<SBEntry> bones = new List<SBEntry>();

        public SB()
        {
            ImageKey = "skeleton";
            SelectedImageKey = "skeleton";
            Text = "model.sb";

            ContextMenu = new ContextMenu();
            MenuItem OpenEdit = new MenuItem("Open Editor");
            OpenEdit.Click += OpenEditor;
            ContextMenu.MenuItems.Add(OpenEdit);
        }

        public void TryGetEntry(uint hash, out SBEntry entry)
        {
            entry = null;
            foreach (SBEntry sb in bones)
                if (sb.hash == hash)
                    entry = sb;
        }

        public void OpenEditor(object sender, EventArgs args)
        {
            SwagEditor swagEditor = new SwagEditor(this);
            swagEditor.Text = Path.GetFileName(FilePath);
            swagEditor.FilePath = FilePath;
            MainForm.Instance.AddDockedControl(swagEditor);
        }

        public override void Read(string filename)
        {
            FileData d = new FileData(filename);
            FilePath = filename;
            d.endian = Endianness.Little; // characters are little
            d.Seek(8); // skip magic and version?
            int count = d.ReadInt(); // entry count

            for (int i = 0; i < count; i++)
            {
                SBEntry sb = new SBEntry()
                {
                    hash = (uint)d.ReadInt(),
                    param1_1 = d.ReadFloat(),
                    param1_2 = d.ReadInt(),
                    param1_3 = d.ReadInt(),
                    param2_1 = d.ReadFloat(),
                    param2_2 = d.ReadFloat(),
                    param2_3 = d.ReadInt(),
                    rx1 = d.ReadFloat(),
                    rx2 = d.ReadFloat(),
                    ry1 = d.ReadFloat(),
                    ry2 = d.ReadFloat(),
                    rz1 = d.ReadFloat(),
                    rz2 = d.ReadFloat()
                };

                for (int j = 0; j < 8; j++)
                    sb.boneHashes[j] = (uint)d.ReadInt();

                for (int j = 0; j < 4; j++)
                    sb.unks1[j] = d.ReadFloat();

                for (int j = 0; j < 6; j++)
                    sb.unks2[j] = d.ReadFloat();

                sb.factor = d.ReadFloat();

                for (int j = 0; j < 3; j++)
                    sb.ints[j] = d.ReadInt();

                bones.Add(sb);

                /*Console.WriteLine(sb.hash.ToString("x"));
                Console.WriteLine(d.readFloat() + " " + d.readInt() + " " + d.readInt());
                Console.WriteLine(d.readFloat() + " " + d.readInt() + " " + d.readInt());
 
                //28 floats?
                Console.WriteLine(d.readFloat() + " " + d.readFloat());
                Console.WriteLine(d.readFloat() + " " + d.readFloat());
                Console.WriteLine(d.readFloat() + " " + d.readFloat());
                Console.WriteLine(d.readFloat() + " " + d.readFloat() + " " + d.readFloat() + " " + d.readFloat());
                Console.WriteLine(d.readFloat() + " " + d.readFloat() + " " + d.readFloat() + " " + d.readFloat());
 
                Console.WriteLine(d.readFloat() + " " + d.readFloat() + " " + d.readFloat() + " " + d.readFloat());
                Console.WriteLine(d.readFloat() + " " + d.readFloat() + " " + d.readFloat() + " " + d.readFloat());
 
                Console.WriteLine(d.readFloat() + " " + d.readFloat());
                Console.WriteLine(d.readInt() +  " " + d.readInt());
                Console.WriteLine(d.readInt() + " " + d.readInt());
                Console.WriteLine();*/
            }
        }

        public override byte[] Rebuild()
        {
            FileOutput o = new FileOutput();
            o.endian = Endianness.Little;

            o.WriteString(" BWS");
            o.WriteShort(0x05);
            o.WriteShort(0x01);
            o.WriteInt(bones.Count);

            foreach (SBEntry s in bones)
            {
                o.WriteInt((int)s.hash);
                o.WriteFloat(s.param1_1);
                o.WriteInt(s.param1_2);
                o.WriteInt(s.param1_3);
                o.WriteFloat(s.param2_1);
                o.WriteFloat(s.param2_2);
                o.WriteInt(s.param2_3);
                o.WriteFloat(s.rx1);
                o.WriteFloat(s.rx2);
                o.WriteFloat(s.ry1);
                o.WriteFloat(s.ry2);
                o.WriteFloat(s.rz1);
                o.WriteFloat(s.rz2);

                for (int j = 0; j < 8; j++)
                    o.WriteInt((int)s.boneHashes[j]);

                for (int j = 0; j < 4; j++)
                    o.WriteFloat(s.unks1[j]);

                for (int j = 0; j < 6; j++)
                    o.WriteFloat(s.unks2[j]);

                o.WriteFloat(s.factor);

                for (int j = 0; j < 3; j++)
                    o.WriteInt(s.ints[j]);
            }

            return o.GetBytes();
        }
    }
}

namespace SmashForge
{
    public class csvHashes
    {
        public Dictionary<string, uint> names = new Dictionary<string, uint>();
        public Dictionary<uint, string> ids = new Dictionary<uint, string>();

        public csvHashes(string filename)
        {
            var reader = new StreamReader(File.OpenRead(filename));

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                names.Add(values[0], Convert.ToUInt32(values[1]));
                ids.Add(Convert.ToUInt32(values[1]), values[0]);
            }
        }
    }
}