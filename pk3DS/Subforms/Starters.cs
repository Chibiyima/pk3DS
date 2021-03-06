﻿using System;
using System.IO;
using System.Windows.Forms;

namespace pk3DS
{
    public partial class Starters : Form
    {
        private byte[][] personal;
        public Starters()
        {
            specieslist[0] = "---";
            Array.Resize(ref specieslist, 722);

            string[] personalList = Directory.GetFiles("personal");
            personal = new byte[personalList.Length][];
            for (int i = 0; i < personalList.Length; i++)
                personal[i] = File.ReadAllBytes("personal" + Path.DirectorySeparatorChar + i.ToString("000") + ".bin");
            if (!File.Exists(CROPath))
            {
                Util.Error("CRO does not exist! Closing.", CROPath);
                Close();
            }
            InitializeComponent();

            // 2 sets of Starters for X/Y
            // 4 sets of Starters for OR/AS
            Choices = new[]
            {
                new[] {CB_G1_0, CB_G1_1, CB_G1_2},
                new[] {CB_G2_0, CB_G2_1, CB_G2_2},
                new[] {CB_G3_0, CB_G3_1, CB_G3_2},
                new[] {CB_G4_0, CB_G4_1, CB_G4_2},
            };
            Previews = new[]
            {
                new[] {PB_G1_0, PB_G1_1, PB_G1_2},
                new[] {PB_G2_0, PB_G2_1, PB_G2_2},
                new[] {PB_G3_0, PB_G3_1, PB_G3_2},
                new[] {PB_G4_0, PB_G4_1, PB_G4_2},
            };
            Labels = new[] { L_Set1, L_Set2, L_Set3, L_Set4 };

            Width = Main.oras ? Width : Width/2 + 2;
            loadData();
        }
        internal static string CROPath = Path.Combine(Main.RomFSPath, "DllPoke3Select.cro");
        private string[] specieslist = Main.getText((Main.oras) ? 98 : 80);
        private ComboBox[][] Choices;
        private PictureBox[][] Previews;
        private Label[] Labels;
        private string[] StarterSummary = Main.oras
            ? new[] { "Gen 3 Starters", "Gen 2 Starters", "Gen 4 Starters", "Gen 5 Starters" }
            : new[] { "Gen 6 Starters", "Gen 1 Starters" };
        private byte[] Data;
        private int Count = Main.oras ? 4 : 2;
        private int offset = Main.oras ? 0x9FFC : 0x8E58;
        private void B_Save_Click(object sender, EventArgs e)
        {
            saveData();
            Close();
        }
        private void B_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void loadData()
        {
            Data = File.ReadAllBytes(CROPath);
            for (int i = 0; i < Count; i++)
            {
                Labels[i].Visible = true;
                Labels[i].Text = StarterSummary[i];
                for (int j = 0; j < 3; j++)
                {
                    foreach (string s in specieslist)
                        Choices[i][j].Items.Add(s);
                    int species = BitConverter.ToUInt16(Data, offset + (i*3 + j)*0x54);
                    Choices[i][j].SelectedIndex = species; // changing index prompts loading of sprite

                    Choices[i][j].Visible = Previews[i][j].Visible = true;
                }
            }
        }
        private void saveData()
        {
            for (int i = 0; i < Count; i++)
                for (int j = 0; j < 3; j++)
                    Array.Copy(
                        BitConverter.GetBytes((ushort) Choices[i][j].SelectedIndex), 0, 
                        Data, offset + (i*3 + j)*0x54, 2);

            File.WriteAllBytes(CROPath, Data);
        }

        private void changeSpecies(object sender, EventArgs e)
        {
            // Fetch the corresponding PictureBox to update
            string name = (sender as ComboBox).Name;
            int group = Int32.Parse(name[4]+"") - 1;
            int index = Int32.Parse(name[6]+"");

            int species = (sender as ComboBox).SelectedIndex;
            Previews[group][index].Image = Util.scaleImage(Util.getSprite(species, 0, 0, 0), 3);
        }

        private void B_Randomize_Click(object sender, EventArgs e)
        {
            bool blind = DialogResult.Yes ==
                         Util.Prompt(MessageBoxButtons.YesNo, "Hide randomization, save, and close?",
                             "If you want the Starters to be a surprise :)");
            if (blind)
                Hide();

            // Iterate for each group of Starters
            for (int i = 0; i < Count; i++)
            {
                // Get Species List
                int gen = Int32.Parse(Labels[i].Text[4]+"");
                int[] sL = CHK_Gen.Checked
                    ? Randomizer.getSpeciesList(gen==1, gen==2, gen==3, gen==4, gen==5, gen==6, false, false, false)
                    : Randomizer.getSpeciesList(true, true, true, true, true, true, false, false, false);
                int ctr = 0;
                // Assign Species
                for (int j = 0; j < 3; j++)
                {
                    int species = Randomizer.getRandomSpecies(ref sL, ref ctr);

                    if (CHK_BST.Checked) // Enforce BST
                    {
                        PersonalInfo oldpkm = new PersonalInfo(personal[BitConverter.ToUInt16(Data, offset + (i * 3 + j) * 0x54)]); // Use original species cuz why not.
                        PersonalInfo pkm = new PersonalInfo(personal[species]);

                        while (!(pkm.BST * 5 / 6 < oldpkm.BST && pkm.BST * 6 / 5 > oldpkm.BST))
                        { species = Randomizer.getRandomSpecies(ref sL, ref ctr); pkm = new PersonalInfo(personal[species]); }
                    }

                    Choices[i][j].SelectedIndex = species;
                }
            }

            if (blind)
            {
                saveData();
                Close();
            }
        }
    }
}