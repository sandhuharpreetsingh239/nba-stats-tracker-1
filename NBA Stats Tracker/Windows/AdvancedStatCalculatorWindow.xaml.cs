﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Ciloci.Flee;
using LeftosCommonLibrary;
using NBA_Stats_Tracker.Data;
using NBA_Stats_Tracker.Data.BoxScores;
using NBA_Stats_Tracker.Data.Misc;
using NBA_Stats_Tracker.Data.Players;
using NBA_Stats_Tracker.Data.SQLiteIO;
using NBA_Stats_Tracker.Data.Teams;
using NBA_Stats_Tracker.Helper.EventHandlers;
using NBA_Stats_Tracker.Helper.Misc;
using NBA_Stats_Tracker.Helper.Miscellaneous;

namespace NBA_Stats_Tracker.Windows
{
    /// <summary>
    /// Interaction logic for AdvancedStatCalculatorWindow.xaml
    /// </summary>
    public partial class AdvancedStatCalculatorWindow : Window
    {
        private bool changingTimeframe;

        private ObservableCollection<KeyValuePair<int, string>> PlayersList
        {
            get { return _playersList; }
            set
            {
                _playersList = value;
                OnPropertyChanged("PlayersList");
            }
        }

        private ObservableCollection<PlayerStatsRow> psrList { get; set; }
        private ObservableCollection<TeamStatsRow> tsrList { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private readonly List<string> Positions = new List<string> {"Any", " ", "PG", "SG", "SF", "PF", "C"};

        private readonly List<string> Totals = new List<string>
                                               {
                                                   " ",
                                                   "PTS (PF)",
                                                   "PA",
                                                   "FGM",
                                                   "FGA",
                                                   "3PM",
                                                   "3PA",
                                                   "FTM",
                                                   "FTA",
                                                   "REB",
                                                   "OREB",
                                                   "AST",
                                                   "STL",
                                                   "BLK",
                                                   "TO",
                                                   "FOUL"
                                               };

        private readonly List<string> NumericOptions = new List<string> {"<", "<=", "=", ">=", ">"};
        private ObservableCollection<KeyValuePair<int, string>> _playersList;
        private bool loading;
        private int curSeason;

        public enum SelectionType
        {
            Team,
            Player
        };

        public struct Selection
        {
            public SelectionType SelectionType;
            public int ID;

            public Selection(SelectionType selectionType, int ID)
            {
                SelectionType = selectionType;
                this.ID = ID;
            }
        };

        public struct Filter
        {
            public string Parameter;
            public string Operator;
            public string Value;

            public Filter(string parameter, string oper, string value)
            {
                Parameter = parameter;
                Operator = oper;
                Value = value;
            }
        }

        private Dictionary<Selection, List<Filter>> filters = new Dictionary<Selection, List<Filter>>();

        public AdvancedStatCalculatorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            changingTimeframe = true;
            dtpEnd.SelectedDate = MainWindow.tf.EndDate;
            dtpStart.SelectedDate = MainWindow.tf.StartDate;
            cmbSeasonNum.ItemsSource = MainWindow.mwInstance.cmbSeasonNum.ItemsSource;
            cmbSeasonNum.SelectedItem = MainWindow.SeasonList.Single(pair => pair.Key == MainWindow.tf.SeasonNum);
            cmbTFSeason.ItemsSource = MainWindow.mwInstance.cmbSeasonNum.ItemsSource;
            cmbTFSeason.SelectedItem = cmbSeasonNum.SelectedItem;
            if (MainWindow.tf.isBetween)
            {
                rbStatsBetween.IsChecked = true;
            }
            else
            {
                rbStatsAllTime.IsChecked = true;
            }
            changingTimeframe = false;

            loading = true;
            cmbPosition1Filter.ItemsSource = Positions;
            cmbPosition2Filter.ItemsSource = Positions;
            cmbPosition1Filter.SelectedIndex = 0;
            cmbPosition2Filter.SelectedIndex = 0;
            cmbTotalsPar.ItemsSource = Totals;
            cmbTotalsOp.ItemsSource = NumericOptions;
            cmbTotalsPar.SelectedIndex = 0;
            cmbTotalsOp.SelectedIndex = 3;
            loading = false;

            PopulateTeamsCombo();
        }

        private void PopulateTeamsCombo()
        {
            cmbTeamFilter.Items.Clear();
            cmbSelectedTeam.Items.Clear();
            List<string> teams = new List<string>();
            MainWindow.tst.Values.ToList().ForEach(ts => teams.Add(ts.displayName));
            teams.Sort();
            cmbTeamFilter.Items.Add("- Any -");
            teams.ForEach(delegate(string i)
                          {
                              cmbTeamFilter.Items.Add(i);
                              cmbSelectedTeam.Items.Add(i);
                          });

            cmbTeamFilter.SelectedIndex = -1;
            cmbTeamFilter.SelectedIndex = 0;
            cmbPosition1Filter.SelectedIndex = 0;
            cmbPosition2Filter.SelectedIndex = 0;
        }

        private void cmbTeamFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulatePlayersCombo();
        }

        private void PopulatePlayersCombo()
        {
            PlayersList = new ObservableCollection<KeyValuePair<int, string>>();
            List<PlayerStats> list = MainWindow.pst.Values.ToList();
            if (cmbPosition1Filter.SelectedItem != null && cmbPosition1Filter.SelectedItem.ToString() != "Any")
            {
                list = list.Where(ps => ps.Position1.ToString() == cmbPosition1Filter.SelectedItem.ToString()).ToList();
            }
            if (cmbPosition2Filter.SelectedItem != null && cmbPosition2Filter.SelectedItem.ToString() != "Any")
            {
                list = list.Where(ps => ps.Position2.ToString() == cmbPosition2Filter.SelectedItem.ToString()).ToList();
            }
            list = list.OrderBy(ps => ps.LastName).ThenBy(ps => ps.FirstName).ToList();
            if (chkIsActive.IsChecked.GetValueOrDefault() && cmbTeamFilter.SelectedItem.ToString() != "- Any -")
            {
                list =
                    list.Where(
                        ps =>
                        ps.TeamF == Misc.GetCurTeamFromDisplayName(MainWindow.tst, cmbTeamFilter.SelectedItem.ToString()) && ps.isActive)
                        .ToList();
            }
            else if (chkIsActive.IsChecked == false)
            {
                list = list.Where(ps => !ps.isActive).ToList();
            }
            list.ForEach(
                ps =>
                PlayersList.Add(new KeyValuePair<int, string>(ps.ID,
                                                              String.Format("{0}, {1} ({2})", ps.LastName, ps.FirstName,
                                                                            ps.Position1.ToString()))));

            cmbSelectedPlayer.ItemsSource = PlayersList;
        }

        private void cmbPosition1Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
                PopulatePlayersCombo();
        }

        private void cmbPosition2Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
                PopulatePlayersCombo();
        }

        private void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!changingTimeframe)
            {
                changingTimeframe = true;
                if (cmbSeasonNum.SelectedIndex == -1)
                    return;

                cmbTFSeason.SelectedItem = cmbSeasonNum.SelectedItem;

                curSeason = ((KeyValuePair<int, string>) (((cmbSeasonNum)).SelectedItem)).Key;

                if (MainWindow.tf.SeasonNum != curSeason || MainWindow.tf.isBetween)
                {
                    MainWindow.tf = new Timeframe(curSeason);
                    MainWindow.ChangeSeason(curSeason);
                    SQLiteIO.LoadSeason();
                }

                PopulateTeamsCombo();
                changingTimeframe = false;
            }
        }

        private void cmbTFSeason_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!changingTimeframe)
            {
                changingTimeframe = true;
                if (cmbTFSeason.SelectedIndex == -1)
                    return;

                cmbSeasonNum.SelectedItem = cmbTFSeason.SelectedItem;
                rbStatsAllTime.IsChecked = true;

                curSeason = ((KeyValuePair<int, string>) (((cmbTFSeason)).SelectedItem)).Key;

                if (MainWindow.tf.SeasonNum != curSeason || MainWindow.tf.isBetween)
                {
                    MainWindow.tf = new Timeframe(curSeason);
                    MainWindow.ChangeSeason(curSeason);
                    SQLiteIO.LoadSeason();
                }

                PopulateTeamsCombo();

                changingTimeframe = false;
            }
        }

        private void rbStatsAllTime_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.tf = new Timeframe(curSeason);
            if (!changingTimeframe)
            {
                MainWindow.UpdateAllData();
                cmbSeasonNum_SelectionChanged(null, null);
            }
        }

        private void rbStatsBetween_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.tf = new Timeframe(dtpStart.SelectedDate.GetValueOrDefault(), dtpEnd.SelectedDate.GetValueOrDefault());
            if (!changingTimeframe)
            {
                MainWindow.UpdateAllData();
                PopulateTeamsCombo();
            }
        }

        private void dtpStart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changingTimeframe)
                return;
            changingTimeframe = true;
            if (dtpEnd.SelectedDate < dtpStart.SelectedDate)
            {
                dtpEnd.SelectedDate = dtpStart.SelectedDate.GetValueOrDefault().AddMonths(1).AddDays(-1);
            }
            MainWindow.tf = new Timeframe(dtpStart.SelectedDate.GetValueOrDefault(), dtpEnd.SelectedDate.GetValueOrDefault());
            rbStatsBetween.IsChecked = true;
            changingTimeframe = false;
            MainWindow.UpdateAllData();
            PopulateTeamsCombo();
        }

        private void dtpEnd_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changingTimeframe)
                return;
            changingTimeframe = true;
            if (dtpEnd.SelectedDate < dtpStart.SelectedDate)
            {
                dtpStart.SelectedDate = dtpEnd.SelectedDate.GetValueOrDefault().AddMonths(-1).AddDays(1);
            }
            MainWindow.tf = new Timeframe(dtpStart.SelectedDate.GetValueOrDefault(), dtpEnd.SelectedDate.GetValueOrDefault());
            rbStatsBetween.IsChecked = true;
            changingTimeframe = false;
            MainWindow.UpdateAllData();
            PopulateTeamsCombo();
        }

        private void chkIsActive_Click(object sender, RoutedEventArgs e)
        {
            if (chkIsActive.IsChecked.GetValueOrDefault())
                cmbTeamFilter.SelectedIndex = 0;
            else
                cmbTeamFilter.SelectedIndex = -1;
            PopulatePlayersCombo();
        }

        private void btnTotalsAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSelectedPlayer.SelectedIndex == -1)
            {
                if (cmbSelectedTeam.SelectedIndex == -1)
                {
                    return;
                }
                else
                {
                    if (cmbTotalsPar.SelectedIndex < 1 || cmbTotalsOp.SelectedIndex == -1)
                        return;

                    string team = Misc.GetCurTeamFromDisplayName(MainWindow.tst, cmbSelectedTeam.SelectedItem.ToString());
                    KeyValuePair<Selection, List<Filter>> filter;
                    try
                    {
                        filter =
                            filters.Single(fil => fil.Key.SelectionType == SelectionType.Team && fil.Key.ID == MainWindow.TeamOrder[team]);
                    }
                    catch
                    {
                        filters.Add(new Selection(SelectionType.Team, MainWindow.TeamOrder[team]), new List<Filter>());
                        filter =
                            filters.Single(fil => fil.Key.SelectionType == SelectionType.Team && fil.Key.ID == MainWindow.TeamOrder[team]);
                    }
                    try
                    {
                        filter.Value.Remove(
                            filter.Value.Single(
                                o =>
                                o.Parameter == cmbTotalsPar.SelectedItem.ToString() && o.Operator == cmbTotalsOp.SelectedItem.ToString()));
                    }
                    catch
                    {
                    }
                    finally
                    {
                        filter.Value.Add(new Filter(cmbTotalsPar.SelectedItem.ToString(), cmbTotalsOp.SelectedItem.ToString(),
                                                    txtTotalsVal.Text));
                    }
                }
            }
            else
            {
                if (cmbTotalsPar.SelectedIndex == -1 || cmbTotalsOp.SelectedIndex == -1)
                    return;

                int playerID = ((KeyValuePair<int, string>) (((cmbSelectedPlayer)).SelectedItem)).Key;
                KeyValuePair<Selection, List<Filter>> filter;
                try
                {
                    filter = filters.Single(fil => fil.Key.SelectionType == SelectionType.Player && fil.Key.ID == playerID);
                }
                catch
                {
                    filters.Add(new Selection(SelectionType.Player, playerID), new List<Filter>());
                    filter = filters.Single(fil => fil.Key.SelectionType == SelectionType.Player && fil.Key.ID == playerID);
                }
                if (cmbTotalsPar.SelectedIndex > 0)
                {
                    if (cmbTotalsPar.SelectedItem.ToString() == "PA")
                        return;

                    try
                    {
                        filter.Value.Remove(
                            filter.Value.Single(
                                o =>
                                o.Parameter == cmbTotalsPar.SelectedItem.ToString() && o.Operator == cmbTotalsOp.SelectedItem.ToString()));
                    }
                    catch
                    {
                    }
                    finally
                    {
                        filter.Value.Add(new Filter(cmbTotalsPar.SelectedItem.ToString(), cmbTotalsOp.SelectedItem.ToString(),
                                                    txtTotalsVal.Text));
                    }
                }
                else
                {
                    try
                    {
                        filter.Value.Remove(filter.Value.Single(f => f.Parameter == "isStarter"));
                    }
                    catch
                    {
                    }
                    finally
                    {
                        string value;
                        if (chkIsStarter.IsChecked == null)
                        {
                            value = "Any";
                        }
                        else if (chkIsStarter.IsChecked == true)
                        {
                            value = "True";
                        }
                        else
                        {
                            value = "False";
                        }
                        filter.Value.Add(new Filter("isStarter", "is", value));
                    }

                    try
                    {
                        filter.Value.Remove(filter.Value.Single(f => f.Parameter == "isInjured"));
                    }
                    catch
                    {
                    }
                    finally
                    {
                        string value;
                        if (chkIsInjured.IsChecked == null)
                        {
                            value = "Any";
                        }
                        else if (chkIsInjured.IsChecked == true)
                        {
                            value = "True";
                        }
                        else
                        {
                            value = "False";
                        }
                        filter.Value.Add(new Filter("isInjured", "is", value));
                    }

                    try
                    {
                        filter.Value.Remove(filter.Value.Single(f => f.Parameter == "isOut"));
                    }
                    catch
                    {
                    }
                    finally
                    {
                        string value;
                        if (chkIsOut.IsChecked == null)
                        {
                            value = "Any";
                        }
                        else if (chkIsOut.IsChecked == true)
                        {
                            value = "True";
                        }
                        else
                        {
                            value = "False";
                        }
                        filter.Value.Add(new Filter("isOut", "is", value));
                    }
                }
            }
            PopulateTotalsList();
        }

        private void PopulateTotalsList()
        {
            lstTotals.Items.Clear();
            foreach (var filter in filters)
            {
                string s;
                if (filter.Key.SelectionType == SelectionType.Team)
                {
                    s = string.Format("(#T{0}) {1}: ", filter.Key.ID, MainWindow.tst.Values.Single(ts => ts.ID == filter.Key.ID).displayName);
                }
                else
                {
                    var player = MainWindow.pst.Values.Single(ps => ps.ID == filter.Key.ID);
                    string teamName;
                    try
                    {
                        teamName = Misc.GetDisplayNameFromTeam(MainWindow.tst, player.TeamF);
                    }
                    catch (Exception)
                    {
                        teamName = "Inactive";
                    }
                    s = String.Format("(#P{4}) {0}, {1} ({2} - {3}): ", player.LastName, player.FirstName, player.Position1, teamName,
                                      player.ID);
                }
                foreach (var option in filter.Value)
                {
                    string s2 = s + String.Format("{0} {1} {2}", option.Parameter, option.Operator, option.Value);
                    lstTotals.Items.Add(s2);
                }
            }
        }

        private void cmbSelectedPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSelectedPlayer.SelectedIndex == -1)
                return;

            cmbSelectedTeam.SelectedIndex = -1;
        }

        private void cmbSelectedTeam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSelectedTeam.SelectedIndex == -1)
                return;

            cmbSelectedPlayer.SelectedIndex = -1;
        }

        private void btnTotalsDel_Click(object sender, RoutedEventArgs e)
        {
            if (lstTotals.SelectedIndex == -1)
                return;

            foreach (string item in lstTotals.SelectedItems)
            {
                int id = Convert.ToInt32(item.Substring(3).Split(')')[0]);
                string parameter = item.Split(':')[1].Trim().Split(' ')[0];
                KeyValuePair<Selection, List<Filter>> filter;
                if (item.Substring(2, 1) == "T")
                {
                    filter = filters.Single(f => f.Key.SelectionType == SelectionType.Team && f.Key.ID == id);
                }
                else
                {
                    filter = filters.Single(f => f.Key.SelectionType == SelectionType.Player && f.Key.ID == id);
                }
                filter.Value.Remove(filter.Value.Single(o => o.Parameter == parameter));
            }

            PopulateTotalsList();
        }

        /// <summary>
        /// Handles the LoadingRow event of the dg control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridRowEventArgs" /> instance containing the event data.</param>
        private void dg_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void dgvTeamStats_Sorting(object sender, DataGridSortingEventArgs e)
        {
            EventHandlers.StatColumn_Sorting(e);
        }

        private void dgvPlayerStats_Sorting(object sender, DataGridSortingEventArgs e)
        {
            EventHandlers.StatColumn_Sorting(e);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<int, TeamStats> advtst = new Dictionary<int, TeamStats>();
            Dictionary<int, TeamStats> advtstopp = new Dictionary<int, TeamStats>();
            Dictionary<int, PlayerStats> advpst = new Dictionary<int, PlayerStats>();
            SortedDictionary<string, int> advTeamOrder = new SortedDictionary<string, int>();

            Dictionary<int, TeamStats> advtst_not = new Dictionary<int, TeamStats>();
            Dictionary<int, TeamStats> advtstopp_not = new Dictionary<int, TeamStats>();
            Dictionary<int, PlayerStats> advpst_not = new Dictionary<int, PlayerStats>();
            SortedDictionary<string, int> advTeamOrder_not = new SortedDictionary<string, int>();

            var bsToCalculate = new List<BoxScoreEntry>();
            var notBsToCalculate = new List<BoxScoreEntry>();
            foreach (var bse in MainWindow.bshist)
            {
                bool keep = true;
                foreach (var filter in filters)
                {
                    if (filter.Key.SelectionType == SelectionType.Team)
                    {
                        var teamName = MainWindow.TeamOrder.Single(pair => pair.Value == filter.Key.ID).Key;
                        if (bse.bs.Team1 != teamName && bse.bs.Team2 != teamName)
                        {
                            keep = false;
                            break;
                        }
                        string p = bse.bs.Team1 == teamName ? "1" : "2";

                        foreach (var option in filter.Value)
                        {
                            string parameter;
                            parameter = option.Parameter.Replace("PTS (PF)", "PF");
                            parameter = parameter.Replace("3P", "TP");
                            parameter = parameter.Replace("TO", "TOS");
                            var context = new ExpressionContext();
                            IGenericExpression<bool> ige =
                                context.CompileGeneric<bool>(string.Format("{0} {1} {2}",
                                                                           bse.bs.GetType()
                                                                              .GetProperty(parameter + p)
                                                                              .GetValue(bse.bs, null), option.Operator, option.Value));
                            if (ige.Evaluate() == false)
                            {
                                keep = false;
                                notBsToCalculate.Add(bse);
                                break;
                            }
                        }
                        if (!keep)
                        {
                            break;
                        }
                    }
                    else
                    {
                        PlayerBoxScore pbs;
                        try
                        {
                            pbs = bse.pbsList.Single(pbs1 => pbs1.PlayerID == filter.Key.ID);
                        }
                        catch
                        {
                            keep = false;
                            break;
                        }

                        foreach (var option in filter.Value)
                        {
                            string parameter;
                            parameter = option.Parameter.Replace("PTS (PF)", "PTS");
                            parameter = parameter.Replace("3P", "TP");
                            parameter = parameter.Replace("TO", "TOS");
                            var context = new ExpressionContext();
                            IGenericExpression<bool> ige =
                                context.CompileGeneric<bool>(string.Format("{0} {1} {2}",
                                                                           pbs.GetType().GetProperty(parameter).GetValue(pbs, null),
                                                                           option.Operator, option.Value));
                            if (ige.Evaluate() == false)
                            {
                                keep = false;
                                notBsToCalculate.Add(bse);
                                break;
                            }
                        }
                        if (!keep)
                        {
                            break;
                        }
                    }
                }
                if (keep)
                {
                    bsToCalculate.Add(bse);
                }
            }

            foreach (var bse in bsToCalculate)
            {
                string team1 = bse.bs.Team1;
                string team2 = bse.bs.Team2;
                TeamBoxScore bs = bse.bs;
                if (advtst.Values.All(ts => ts.name != team1))
                {
                    advTeamOrder.Add(team1, advTeamOrder.Any() ? advTeamOrder.Values.Max() + 1 : 0);
                    advtst.Add(advTeamOrder[team1], new TeamStats {ID = advTeamOrder[team1], name = team1, displayName = team1});
                    advtstopp.Add(advTeamOrder[team1], new TeamStats {ID = advTeamOrder[team1], name = team1, displayName = team1});
                }
                var ts1 = advtst.Single(pair => pair.Value.name == team1).Value;
                var ts1opp = advtstopp.Single(pair => pair.Value.name == team1).Value;
                if (advtst.Values.All(ts => ts.name != team2))
                {
                    advTeamOrder.Add(team2, advTeamOrder.Any() ? advTeamOrder.Values.Max() + 1 : 0);
                    advtst.Add(advTeamOrder[team2], new TeamStats {ID = advTeamOrder[team2], name = team2, displayName = team2});
                    advtstopp.Add(advTeamOrder[team2], new TeamStats {ID = advTeamOrder[team2], name = team2, displayName = team2});
                }
                var ts2 = advtst.Single(pair => pair.Value.name == team2).Value;
                var ts2opp = advtstopp.Single(pair => pair.Value.name == team2).Value;
                TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2, true);
                TeamStats.AddTeamStatsFromBoxScore(bs, ref ts2opp, ref ts1opp, true);
                foreach (var pbs in bse.pbsList)
                {
                    if (advpst.All(pair => pair.Key != pbs.PlayerID))
                    {
                        advpst.Add(pbs.PlayerID, MainWindow.pst[pbs.PlayerID].DeepClone());
                        advpst[pbs.PlayerID].ResetStats();
                    }
                    advpst[pbs.PlayerID].AddBoxScore(pbs, false);
                }
            }

            foreach (var bse in notBsToCalculate)
            {
                string team1 = bse.bs.Team1;
                string team2 = bse.bs.Team2;
                TeamBoxScore bs = bse.bs;
                if (advtst_not.Values.All(ts => ts.name != team1))
                {
                    advTeamOrder_not.Add(team1, advTeamOrder_not.Any() ? advTeamOrder_not.Values.Max() + 1 : 0);
                    advtst_not.Add(advTeamOrder_not[team1], new TeamStats {ID = advTeamOrder_not[team1], name = team1, displayName = team1});
                    advtstopp_not.Add(advTeamOrder_not[team1],
                                      new TeamStats {ID = advTeamOrder_not[team1], name = team1, displayName = team1});
                }
                var ts1 = advtst_not.Single(pair => pair.Value.name == team1).Value;
                var ts1opp = advtstopp_not.Single(pair => pair.Value.name == team1).Value;
                if (advtst_not.Values.All(ts => ts.name != team2))
                {
                    advTeamOrder_not.Add(team2, advTeamOrder_not.Any() ? advTeamOrder_not.Values.Max() + 1 : 0);
                    advtst_not.Add(advTeamOrder_not[team2], new TeamStats {ID = advTeamOrder_not[team2], name = team2, displayName = team2});
                    advtstopp_not.Add(advTeamOrder_not[team2],
                                      new TeamStats {ID = advTeamOrder_not[team2], name = team2, displayName = team2});
                }
                var ts2 = advtst_not.Single(pair => pair.Value.name == team2).Value;
                var ts2opp = advtstopp_not.Single(pair => pair.Value.name == team2).Value;
                TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2, true);
                TeamStats.AddTeamStatsFromBoxScore(bs, ref ts2opp, ref ts1opp, true);
                foreach (var pbs in bse.pbsList)
                {
                    if (pbs.isOut)
                        continue;

                    if (advpst_not.All(pair => pair.Key != pbs.PlayerID))
                    {
                        advpst_not.Add(pbs.PlayerID, MainWindow.pst[pbs.PlayerID].DeepClone());
                        advpst_not[pbs.PlayerID].ResetStats();
                    }
                    advpst_not[pbs.PlayerID].AddBoxScore(pbs, false);
                }
            }

            TeamStats.CalculateAllMetrics(ref advtst, advtstopp, false);
            advpst.ToList().ForEach(pair => PlayerStats.CalculateRates(pair.Value.stats, ref pair.Value.metrics));

            psrList = new ObservableCollection<PlayerStatsRow>();
            advpst.ToList().ForEach(pair => psrList.Add(new PlayerStatsRow(pair.Value)));
            tsrList = new ObservableCollection<TeamStatsRow>();
            advtst.ToList().ForEach(pair => tsrList.Add(new TeamStatsRow(pair.Value)));

            dgvTeamStats.ItemsSource = tsrList;
            dgvPlayerStats.ItemsSource = psrList;

            TeamStats.CalculateAllMetrics(ref advtst_not, advtstopp_not, false);
            advpst_not.ToList().ForEach(pair => PlayerStats.CalculateRates(pair.Value.stats, ref pair.Value.metrics));

            psrList_not = new ObservableCollection<PlayerStatsRow>();
            advpst_not.ToList().ForEach(pair => psrList_not.Add(new PlayerStatsRow(pair.Value)));
            tsrList_not = new ObservableCollection<TeamStatsRow>();
            advtst_not.ToList().ForEach(pair => tsrList_not.Add(new TeamStatsRow(pair.Value)));

            dgvTeamStatsOpposite.ItemsSource = tsrList_not;
            dgvPlayerStatsOpposite.ItemsSource = psrList_not;

            tbcAdv.SelectedItem = tabPlayerStats;
        }

        protected ObservableCollection<TeamStatsRow> tsrList_not { get; set; }

        protected ObservableCollection<PlayerStatsRow> psrList_not { get; set; }
    }
}