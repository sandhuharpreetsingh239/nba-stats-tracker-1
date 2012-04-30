﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NBA_2K12_Correct_Team_Stats
{
    /// <summary>
    /// Interaction logic for playerOverviewW.xaml
    /// </summary>
    public partial class playerOverviewW : Window
    {
        private SQLiteDatabase db = new SQLiteDatabase(MainWindow.currentDB);
        private ObservableCollection<KeyValuePair<int, string>> _playersList = new ObservableCollection<KeyValuePair<int, string>>();
        private PlayerStatsRow psr;
        private DataTable dt_ov;
        private int curSeason = MainWindow.curSeason;
        private int maxSeason = MainWindow.getMaxSeason(MainWindow.currentDB);
        private string playersT = "Players";
        private Dictionary<int, PlayerStats> playersActive;
        private Dictionary<int, PlayerStats> playersSamePosition;
        private Dictionary<int, PlayerStats> playersSameTeam;
        private List<string> Teams;
        private PlayerRankings rankingsActive;
        private PlayerRankings rankingsTeam;
        private PlayerRankings rankingsPosition;
        private int SelectedPlayerID = -1;
        public static string askedTeam;
        private SortedDictionary<string, int> teamOrder = MainWindow.TeamOrder; 

        public const int pGP = 0,
                         pGS = 1,
                         pMINS = 2,
                         pPTS = 3,
                         pDREB = 4,
                         pOREB = 5,
                         pAST = 6,
                         pSTL = 7,
                         pBLK = 8,
                         pTO = 9,
                         pFOUL = 10,
                         pFGM = 11,
                         pFGA = 12,
                         pTPM = 13,
                         pTPA = 14,
                         pFTM = 15,
                         pFTA = 16;

        public const int pMPG = 0,
                         pPPG = 1,
                         pDRPG = 2,
                         pORPG = 3,
                         pAPG = 4,
                         pSPG = 5,
                         pBPG = 6,
                         pTPG = 7,
                         pFPG = 8,
                         pFGp = 9,
                         pFGeff = 10,
                         pTPp = 11,
                         pTPeff = 12,
                         pFTp = 13,
                         pFTeff = 14,
                         pRPG = 15;

        public ObservableCollection<KeyValuePair<int,string>> PlayersList
        {
            get { return _playersList; }
            set 
            { 
                _playersList = value;
                OnPropertyChanged("PlayersList");
            }
        }

        private string _selectedPlayer;
        public string SelectedPlayer
        {
            get { return _selectedPlayer; }
            set { _selectedPlayer = value;
                OnPropertyChanged("SelectedPlayer");
            }
        }

        public playerOverviewW()
        {
            InitializeComponent();

            prepareWindow();
        }

        public playerOverviewW(string team, int playerID) : this()
        {
            cmbTeam.SelectedItem = team;
            cmbPlayer.SelectedValue = playerID.ToString();
        }

        private void prepareWindow()
        {
            DataContext = this;

            PopulateSeasonCombo();

            var Positions = new List<string>() {"PG", "SG", "SF", "PF", "C"};
            var Positions2 = new List<string>() { " ", "PG", "SG", "SF", "PF", "C" };
            cmbPosition1.ItemsSource = Positions;
            cmbPosition2.ItemsSource = Positions2;

            Teams = new List<string>();
            foreach (var kvp in teamOrder)
            {
                Teams.Add(kvp.Key);
            }
            Teams.Add("- Inactive -");

            cmbTeam.ItemsSource = Teams;

            dt_ov = new DataTable();
            dt_ov.Columns.Add("Type");
            dt_ov.Columns.Add("GP");
            dt_ov.Columns.Add("GS");
            dt_ov.Columns.Add("MINS");
            dt_ov.Columns.Add("PTS");
            dt_ov.Columns.Add("FG");
            dt_ov.Columns.Add("FGeff");
            dt_ov.Columns.Add("3PT");
            dt_ov.Columns.Add("3Peff");
            dt_ov.Columns.Add("FT");
            dt_ov.Columns.Add("FTeff");
            dt_ov.Columns.Add("REB");
            dt_ov.Columns.Add("OREB");
            dt_ov.Columns.Add("DREB");
            dt_ov.Columns.Add("AST");
            dt_ov.Columns.Add("TO");
            dt_ov.Columns.Add("STL");
            dt_ov.Columns.Add("BLK");
            dt_ov.Columns.Add("FOUL");

            GetActivePlayers();
        }

        private void GetActivePlayers()
        {
            playersActive = new Dictionary<int, PlayerStats>();

            string q = "select * from " + playersT + " where isActive LIKE 'True'";
            DataTable res = db.GetDataTable(q);
            foreach (DataRow r in res.Rows)
            {
                PlayerStats ps = new PlayerStats(r);
                playersActive.Add(ps.ID, ps);
            }

            rankingsActive = new PlayerRankings(playersActive);
        }

        private void PopulateSeasonCombo()
        {
            for (int i = maxSeason; i > 0; i--)
            {
                cmbSeasonNum.Items.Add(i.ToString());
            }

            cmbSeasonNum.SelectedItem = curSeason.ToString();
        }

        private void cmbTeam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dgvOverviewStats.DataContext = null;
            grdOverview.DataContext = null;
            cmbPosition1.SelectedIndex = -1;
            cmbPosition2.SelectedIndex = -1;

            if (cmbTeam.SelectedIndex == -1) return;

            cmbPlayer.ItemsSource = null;

            PlayersList = new ObservableCollection<KeyValuePair<int, string>>();
            string q;
            if (cmbTeam.SelectedItem.ToString() != "- Inactive -")
            {
                q = "select * from " + playersT + " where TeamFin LIKE '" + cmbTeam.SelectedItem.ToString() + "' AND isActive LIKE 'True'";
            }
            else
            {
                q = "select * from " + playersT + " where isActive LIKE 'False'";
            }
            var res = db.GetDataTable(q);

            playersSameTeam = new Dictionary<int, PlayerStats>();

            foreach (DataRow r in res.Rows)
            {
                PlayersList.Add(new KeyValuePair<int, string>(StatsTracker.getInt(r, "ID"),
                                StatsTracker.getString(r, "FirstName") + " " + StatsTracker.getString(r, "LastName") + 
                                " (" + StatsTracker.getString(r, "Position1") + ")"));
                PlayerStats ps = new PlayerStats(r);
                playersSameTeam.Add(ps.ID, ps);
            }
            rankingsTeam = new PlayerRankings(playersSameTeam);

            cmbPlayer.ItemsSource = PlayersList;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void cmbPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == -1) return;

            SelectedPlayerID =
                ((System.Collections.Generic.KeyValuePair<int, string>)
                 (((System.Windows.Controls.Primitives.Selector) (cmbPlayer)).SelectedItem)).Key;

            string q = "select * from " + playersT + " where ID = " + SelectedPlayerID.ToString();
            
            var res = db.GetDataTable(q);

            if (res.Rows.Count == 0) // Player not found in this year's database
            {
                cmbTeam_SelectionChanged(null, null); // Reload this team's players
                return;
            }

            psr = new PlayerStatsRow(new PlayerStats(res.Rows[0]));

            grdOverview.DataContext = psr;

            q = "select * from " + playersT + " where Position1 LIKE '" + psr.Position1 + "' AND isActive LIKE 'True'";
            res = db.GetDataTable(q);

            playersSamePosition = new Dictionary<int, PlayerStats>();

            foreach (DataRow r in res.Rows)
            {
                PlayerStats ps = new PlayerStats(r);
                playersSamePosition.Add(ps.ID, ps);
            }
            rankingsPosition = new PlayerRankings(playersSamePosition);

            cmbPosition1.SelectedItem = psr.Position1;
            cmbPosition2.SelectedItem = psr.Position2;

            UpdateOverview();
        }

        private void UpdateOverview()
        {
            dt_ov.Clear();

            DataRow dr = dt_ov.NewRow();

            dr["Type"] = "Stats";
            dr["GP"] = psr.GP.ToString();
            dr["GS"] = psr.GS.ToString();
            dr["MINS"] = psr.MINS.ToString();
            dr["PTS"] = psr.PTS.ToString();
            dr["FG"] = psr.FGM.ToString() + "-" + psr.FGA.ToString();
            dr["3PT"] = psr.TPM.ToString() + "-" + psr.TPA.ToString();
            dr["FT"] = psr.FTM.ToString() + "-" + psr.FTA.ToString();
            dr["REB"] = (psr.DREB + psr.OREB).ToString();
            dr["OREB"] = psr.OREB.ToString();
            dr["DREB"] = psr.DREB.ToString();
            dr["AST"] = psr.AST.ToString();
            dr["TO"] = psr.TOS.ToString();
            dr["STL"] = psr.STL.ToString();
            dr["BLK"] = psr.BLK.ToString();
            dr["FOUL"] = psr.FOUL.ToString();

            dt_ov.Rows.Add(dr);

            dr = dt_ov.NewRow();

            dr["Type"] = "Averages";
            dr["MINS"] = String.Format("{0:F1}", psr.MPG);
            dr["PTS"] = String.Format("{0:F1}", psr.PPG);
            dr["FG"] = String.Format("{0:F3}", psr.FGp);
            dr["FGeff"] = String.Format("{0:F2}", psr.FGeff);
            dr["3PT"] = String.Format("{0:F3}", psr.TPp);
            dr["3Peff"] = String.Format("{0:F2}", psr.TPeff);
            dr["FT"] = String.Format("{0:F3}", psr.FTp);
            dr["FTeff"] = String.Format("{0:F2}", psr.FTeff);
            dr["REB"] = String.Format("{0:F1}", psr.RPG);
            dr["OREB"] = String.Format("{0:F1}", psr.ORPG);
            dr["DREB"] = String.Format("{0:F1}", psr.DRPG);
            dr["AST"] = String.Format("{0:F1}", psr.APG);
            dr["TO"] = String.Format("{0:F1}", psr.TPG);
            dr["STL"] = String.Format("{0:F1}", psr.SPG);
            dr["BLK"] = String.Format("{0:F1}", psr.BPG);
            dr["FOUL"] = String.Format("{0:F1}", psr.FPG);

            dt_ov.Rows.Add(dr);

            #region Rankings

            if (psr.isActive)
            {
                int id = Convert.ToInt32(SelectedPlayerID);

                dr = dt_ov.NewRow();

                dr["Type"] = "Rankings";
                dr["MINS"] = String.Format("{0}", rankingsActive.list[id][pMPG]);
                dr["PTS"] = String.Format("{0}", rankingsActive.list[id][pPPG]);
                dr["FG"] = String.Format("{0}", rankingsActive.list[id][pFGp]);
                dr["FGeff"] = String.Format("{0}", rankingsActive.list[id][pFGeff]);
                dr["3PT"] = String.Format("{0}", rankingsActive.list[id][pTPp]);
                dr["3Peff"] = String.Format("{0}", rankingsActive.list[id][pTPeff]);
                dr["FT"] = String.Format("{0}", rankingsActive.list[id][pFTp]);
                dr["FTeff"] = String.Format("{0}", rankingsActive.list[id][pFTeff]);
                dr["REB"] = String.Format("{0}", rankingsActive.list[id][pRPG]);
                dr["OREB"] = String.Format("{0}", rankingsActive.list[id][pORPG]);
                dr["DREB"] = String.Format("{0}", rankingsActive.list[id][pDRPG]);
                dr["AST"] = String.Format("{0}", rankingsActive.list[id][pAPG]);
                dr["TO"] = String.Format("{0}", rankingsActive.list[id][pTPG]);
                dr["STL"] = String.Format("{0}", rankingsActive.list[id][pSPG]);
                dr["BLK"] = String.Format("{0}", rankingsActive.list[id][pBPG]);
                dr["FOUL"] = String.Format("{0}", rankingsActive.list[id][pFPG]);

                dt_ov.Rows.Add(dr);

                dr = dt_ov.NewRow();

                dr["Type"] = "In-team Rankings";
                dr["MINS"] = String.Format("{0}", rankingsTeam.list[id][pMPG]);
                dr["PTS"] = String.Format("{0}", rankingsTeam.list[id][pPPG]);
                dr["FG"] = String.Format("{0}", rankingsTeam.list[id][pFGp]);
                dr["FGeff"] = String.Format("{0}", rankingsTeam.list[id][pFGeff]);
                dr["3PT"] = String.Format("{0}", rankingsTeam.list[id][pTPp]);
                dr["3Peff"] = String.Format("{0}", rankingsTeam.list[id][pTPeff]);
                dr["FT"] = String.Format("{0}", rankingsTeam.list[id][pFTp]);
                dr["FTeff"] = String.Format("{0}", rankingsTeam.list[id][pFTeff]);
                dr["REB"] = String.Format("{0}", rankingsTeam.list[id][pRPG]);
                dr["OREB"] = String.Format("{0}", rankingsTeam.list[id][pORPG]);
                dr["DREB"] = String.Format("{0}", rankingsTeam.list[id][pDRPG]);
                dr["AST"] = String.Format("{0}", rankingsTeam.list[id][pAPG]);
                dr["TO"] = String.Format("{0}", rankingsTeam.list[id][pTPG]);
                dr["STL"] = String.Format("{0}", rankingsTeam.list[id][pSPG]);
                dr["BLK"] = String.Format("{0}", rankingsTeam.list[id][pBPG]);
                dr["FOUL"] = String.Format("{0}", rankingsTeam.list[id][pFPG]);

                dt_ov.Rows.Add(dr);

                dr = dt_ov.NewRow();

                dr["Type"] = "Position Rankings";
                dr["MINS"] = String.Format("{0}", rankingsPosition.list[id][pMPG]);
                dr["PTS"] = String.Format("{0}", rankingsPosition.list[id][pPPG]);
                dr["FG"] = String.Format("{0}", rankingsPosition.list[id][pFGp]);
                dr["FGeff"] = String.Format("{0}", rankingsPosition.list[id][pFGeff]);
                dr["3PT"] = String.Format("{0}", rankingsPosition.list[id][pTPp]);
                dr["3Peff"] = String.Format("{0}", rankingsPosition.list[id][pTPeff]);
                dr["FT"] = String.Format("{0}", rankingsPosition.list[id][pFTp]);
                dr["FTeff"] = String.Format("{0}", rankingsPosition.list[id][pFTeff]);
                dr["REB"] = String.Format("{0}", rankingsPosition.list[id][pRPG]);
                dr["OREB"] = String.Format("{0}", rankingsPosition.list[id][pORPG]);
                dr["DREB"] = String.Format("{0}", rankingsPosition.list[id][pDRPG]);
                dr["AST"] = String.Format("{0}", rankingsPosition.list[id][pAPG]);
                dr["TO"] = String.Format("{0}", rankingsPosition.list[id][pTPG]);
                dr["STL"] = String.Format("{0}", rankingsPosition.list[id][pSPG]);
                dr["BLK"] = String.Format("{0}", rankingsPosition.list[id][pBPG]);
                dr["FOUL"] = String.Format("{0}", rankingsPosition.list[id][pFPG]);

                dt_ov.Rows.Add(dr);
            }

            #endregion

            DataView dv_ov = new DataView(dt_ov);
            dv_ov.AllowNew = false;

            dgvOverviewStats.DataContext = dv_ov;
        }

        private void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            curSeason = Convert.ToInt32(cmbSeasonNum.SelectedItem.ToString());
            MainWindow.curSeason = curSeason;
            playersT = "Players";

            if (curSeason != maxSeason)
            {
                playersT += "S" + curSeason;
            }

            MainWindow.pst = MainWindow.GetPlayersFromDatabase(MainWindow.currentDB, curSeason, maxSeason);

            GetActivePlayers();
            
            if (cmbPlayer.SelectedIndex != -1)
            {
                PlayerStats ps = CreatePlayerStatsFromCurrent();

                string q = "select * from " + playersT + " where ID = " + ps.ID;
                DataTable res = db.GetDataTable(q);

                string newTeam;
                bool nowActive;
                if (res.Rows.Count > 0)
                {
                    nowActive = StatsTracker.getBoolean(res.Rows[0], "isActive");
                    if (nowActive)
                    {
                        newTeam = res.Rows[0]["TeamFin"].ToString();
                    }
                    else
                    {
                        newTeam = " - Inactive -";
                    }
                    cmbTeam.SelectedIndex = -1;
                    if (nowActive)
                    {
                        if (newTeam != "")
                        {
                            cmbTeam.SelectedItem = newTeam;
                        }
                    }
                    else
                    {
                        cmbTeam.SelectedItem = "- Inactive -";
                    }
                    cmbPlayer.SelectedIndex = -1;
                    cmbPlayer.SelectedValue = ps.ID;
                }
                else
                {
                    cmbTeam.SelectedIndex = -1;
                    cmbPlayer.SelectedIndex = -1;
                }
                
            }
        }

        private void btnScoutingReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Player Scouting Reports coming soon!");
        }

        private void btnSavePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == -1) return;

            if (rbStatsBetween.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show(
                    "You can't edit partial stats. You can either edit the total stats (which are kept separately from box-scores"
                    + ") or edit the box-scores themselves.", "NBA Stats Tracker", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var ps = CreatePlayerStatsFromCurrent();

            Dictionary<int, PlayerStats> pslist = new Dictionary<int, PlayerStats>();
            pslist.Add(ps.ID, ps);

            MainWindow.savePlayersToDatabase(MainWindow.currentDB, pslist, curSeason, maxSeason);

            MainWindow.pst = MainWindow.GetPlayersFromDatabase(MainWindow.currentDB, curSeason, maxSeason);

            GetActivePlayers();
            cmbTeam.SelectedIndex = -1;
            if (ps.isActive)
            {
                cmbTeam.SelectedItem = ps.TeamF;
            }
            else
            {
                cmbTeam.SelectedItem = "- Inactive -";
            }
            cmbPlayer.SelectedIndex = -1;
            cmbPlayer.SelectedValue = ps.ID;
            //cmbPlayer.SelectedValue = ps.LastName + " " + ps.FirstName + " (" + ps.Position1 + ")";
        }

        private PlayerStats CreatePlayerStatsFromCurrent()
        {
            if (cmbPosition2.SelectedItem == null) cmbPosition2.SelectedItem = " ";

            string TeamF;
            if (chkIsActive.IsChecked.GetValueOrDefault() == false)
            {
                TeamF = "";
            }
            else
            {
                TeamF = cmbTeam.SelectedItem.ToString();
                if (TeamF == "- Inactive -")
                {
                    askedTeam = "";
                    askTeamW atw = new askTeamW(Teams);
                    atw.ShowDialog();
                    TeamF = askedTeam;
                }
            }

            PlayerStats ps = new PlayerStats(
                psr.ID, txtLastName.Text, txtFirstName.Text, cmbPosition1.SelectedItem.ToString(),
                cmbPosition2.SelectedItem.ToString(), TeamF, psr.TeamS,
                chkIsActive.IsChecked.GetValueOrDefault(), chkIsInjured.IsChecked.GetValueOrDefault(),
                chkIsAllStar.IsChecked.GetValueOrDefault(), chkIsNBAChampion.IsChecked.GetValueOrDefault(),
                dt_ov.Rows[0]);
            return ps;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTeam.SelectedIndex == cmbTeam.Items.Count - 1) cmbTeam.SelectedIndex = 0;
            else cmbTeam.SelectedIndex++;
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTeam.SelectedIndex == 0) cmbTeam.SelectedIndex = cmbTeam.Items.Count - 1;
            else cmbTeam.SelectedIndex--;
        }

        private void btnNextPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == cmbPlayer.Items.Count - 1) cmbPlayer.SelectedIndex = 0;
            else cmbPlayer.SelectedIndex++;
        }

        private void btnPrevPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == 0) cmbPlayer.SelectedIndex = cmbPlayer.Items.Count - 1;
            else cmbPlayer.SelectedIndex--;
        }


    }
}
