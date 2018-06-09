using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data.SQLite;

namespace ScreenRecordPlusChrome
{
    #region useless
    class Fixation //offline calculate fixation and save into DB
    {
        private string _SubjectName;
        private string _DBPath;
        private int _fixationMethod;
        private int _durationThreshold; //ms
        private float _spatialThreshold;
        private SQLiteConnection _sqliteConnect;

        public Fixation(string subject, string path)
        {
            _SubjectName = subject;
            _DBPath = path;
            _sqliteConnect = new SQLiteConnection("Data source=" + _DBPath);
            _sqliteConnect.Open();
        }
        public enum Fixation_Method
        {
            Distance_Dispersion = 0,
            Position_Variance = 1,
            Salvucci_IDT = 2
        }
        public void Calculate(int method, int Dthreshold, float Sthreshold)
        {
            //create fixation table if it's not existed
            //remove data
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS MouseFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS GazeFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM MouseFixation WHERE SubjectName = '" + _SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM GazeFixation WHERE SubjectName = '" + _SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
            }

            _fixationMethod = method;
            _durationThreshold = Dthreshold;
            _spatialThreshold = Sthreshold;
            if (method == 0)
            {
                Distance_Dispersion_Method();
            }
            else if (method == 1)
            {
                Position_Variance_Method();
            }
            else if (method == 2)
            {
                Salvucci_IDT_Method();
            }
        }
        public void CalculateWithURL(int method, int Dthreshold, float Sthreshold, List<int> URLRID)
        {
            //create fixation table if it's not existed
            //remove data
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS MouseFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "CREATE TABLE IF NOT EXISTS GazeFixation (ID integer PRIMARY KEY AUTOINCREMENT, SubjectName varchar(50), StartTime integer, Duration integer, PositionX float, PositionY float, ScrollTopX float, ScrollTopY float, StartID integer, EndID integer, FID integer )";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM MouseFixation WHERE SubjectName = '" + _SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
                _sqliteCmd.CommandText = "DELETE FROM GazeFixation WHERE SubjectName = '" + _SubjectName + "'";
                _sqliteCmd.ExecuteNonQuery();
            }

            _fixationMethod = method;
            _durationThreshold = Dthreshold;
            _spatialThreshold = Sthreshold;
            if (method == 0)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    Tuple<int, int> fid = new Tuple<int, int>(1,1);
                    for (int i = 0; i < URLRID.Count - 1; i++)
                    {
                        fid = Distance_Dispersion_Method_WithURL(URLRID[i], URLRID[i + 1], fid.Item1, fid.Item2);
                    }
                    Distance_Dispersion_Method_WithURL(URLRID[URLRID.Count - 1], -1, fid.Item1, fid.Item2);
                    tr.Commit();
                }
            }
            else if (method == 1)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    Tuple<int, int> fid = new Tuple<int, int>(1, 1);
                    for (int i = 0; i < URLRID.Count - 1; i++)
                    {
                        fid = Position_Variance_Method_WithURL(URLRID[i], URLRID[i + 1], fid.Item1, fid.Item2);
                    }
                    Position_Variance_Method_WithURL(URLRID[URLRID.Count - 1], -1, fid.Item1, fid.Item2);
                    tr.Commit();
                }
            }
            else if (method == 2)
            {
                using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
                {
                    Tuple<int, int> fid = new Tuple<int, int>(1, 1);
                    for (int i = 0; i < URLRID.Count - 1; i++)
                    {
                        fid = Salvucci_IDT_Method_WithURL(URLRID[i], URLRID[i + 1], fid.Item1, fid.Item2);
                    }
                    Salvucci_IDT_Method_WithURL(URLRID[URLRID.Count - 1], -1, fid.Item1, fid.Item2);
                    tr.Commit();
                }
            }
        }

        //Each point in that fixation must be no further than some threshold (dmax) from every other point. 
        private void Distance_Dispersion_Method()
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    int mfid = 1;
                    int gfid = 1;
                    List<MethodItem> Mitems = new List<MethodItem>();
                    List<MethodItem> Gitems = new List<MethodItem>();
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata ORDER BY ID";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        MethodItem mouse = new MethodItem();
                        MethodItem gaze = new MethodItem();

                        if (!rdr.IsDBNull(5))
                        {
                            System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                            mouse._scrolltop.X = s.X;
                            mouse._scrolltop.Y = s.Y;
                            gaze._scrolltop.X = s.X;
                            gaze._scrolltop.Y = s.Y;
                        }
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            mouse._id = rdr.GetInt32(6);
                            mouse._time = rdr.GetInt32(0);
                            mouse._item.X = rdr.GetFloat(1);
                            mouse._item.Y = rdr.GetFloat(2);
                        }
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            gaze._id = rdr.GetInt32(6);
                            gaze._time = rdr.GetInt32(0);
                            gaze._item.X = rdr.GetFloat(3);
                            gaze._item.Y = rdr.GetFloat(4);
                        }

                        #region Mouse
                        //Current point has value
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                int outRangeIndex = -1;
                                for (int i = 0; i < Mitems.Count; i++)
                                {
                                    //Web position
                                    if ((((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y)) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y))) > (_spatialThreshold * _spatialThreshold))
                                    {
                                        outRangeIndex = i;
                                    }
                                }
                                //Distance of current point and certain point in fixation is out of threshold
                                if (outRangeIndex != -1)
                                {
                                    //Time of candidate fixation is longer than threshold
                                    //Save fixation into DB, Clear candidate fixation to 0 
                                    if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                                    {
                                        double centerX = Mitems.Average(center => center._item.X);
                                        double centerY = Mitems.Average(center => center._item.Y);
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                            _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                            _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                            _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                            _sqliteCmd2.ExecuteNonQuery();
                                            mfid++;
                                        }
                                        Mitems.Clear();
                                    }
                                    //Remove 0 to last outRangeIndex from candidate fixation 
                                    else
                                    {
                                        Mitems.RemoveRange(0, outRangeIndex + 1);
                                    }
                                }
                            }
                            //Add current point in fixation
                            Mitems.Add(mouse);
                        }
                        else
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                //Time of candidate fixation is longer than threshold
                                //Save fixation into DB
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                                {
                                    double centerX = Mitems.Average(center => center._item.X);
                                    double centerY = Mitems.Average(center => center._item.Y);
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                        _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                        _sqliteCmd2.ExecuteNonQuery();
                                        mfid++;
                                    }
                                }
                                //Clear candidate fixation to 0 
                                Mitems.Clear();
                            }
                        }
                        #endregion

                        #region Gaze
                        //Current point has value
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                int outRangeIndex = -1;
                                for (int i = 0; i < Gitems.Count; i++)
                                {
                                    if ((((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y)) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y))) > (_spatialThreshold * _spatialThreshold))
                                    {
                                        outRangeIndex = i;
                                    }
                                }
                                //Distance of current point and certain point in fixation is out of threshold
                                if (outRangeIndex != -1)
                                {
                                    //Time of candidate fixation is longer than threshold
                                    //Save fixation into DB, Clear candidate fixation to 0 
                                    if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                                    {
                                        double centerX = Gitems.Average(center => center._item.X);
                                        double centerY = Gitems.Average(center => center._item.Y);
                                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                        {
                                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                            _sqliteCmd2.ExecuteNonQuery();
                                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                            _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                            _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                            _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                            _sqliteCmd2.ExecuteNonQuery();
                                            gfid++;
                                        }
                                        Gitems.Clear();
                                    }
                                    //Remove 0 to last outRangeIndex from candidate fixation 
                                    else
                                    {
                                        Gitems.RemoveRange(0, outRangeIndex + 1);
                                    }
                                }
                            }
                            //Add current point in fixation
                            Gitems.Add(gaze);
                        }
                        else
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                //Time of candidate fixation is longer than threshold
                                //Save fixation into DB, Clear candidate fixation to 0 
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                                {
                                    double centerX = Gitems.Average(center => center._item.X);
                                    double centerY = Gitems.Average(center => center._item.Y);
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                        _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                        _sqliteCmd2.ExecuteNonQuery();
                                        gfid++;
                                    }
                                }
                                Gitems.Clear();
                            }
                        }
                        #endregion

                    }

                    #region Last_fixation
                    //The last fixation
                    //Save fixation into DB
                    if (Mitems.Count != 0)
                    {
                        if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                        {
                            double centerX = Mitems.Average(center => center._item.X);
                            double centerY = Mitems.Average(center => center._item.Y);
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                _sqliteCmd2.ExecuteNonQuery();
                                mfid++;
                            }
                        }
                    }
                    Mitems.Clear();

                    if (Gitems.Count != 0)
                    {
                        if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                        {
                            double centerX = Gitems.Average(center => center._item.X);
                            double centerY = Gitems.Average(center => center._item.Y);
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                _sqliteCmd2.ExecuteNonQuery();
                                gfid++;
                            }
                        }
                    }
                    Gitems.Clear();
                    #endregion

                    rdr.Close();
                }
                tr.Commit();
            }
        }
        private Tuple<int, int> Distance_Dispersion_Method_WithURL(int startID, int endID, int mfid, int gfid)
        {
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                List<MethodItem> Mitems = new List<MethodItem>();
                List<MethodItem> Gitems = new List<MethodItem>();
                if (endID == -1)
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata WHERE ID>=@startid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                }
                else
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata WHERE ID>=@startid AND ID<@endid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                    _sqliteCmd.Parameters.AddWithValue("@endid", endID);
                }
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    MethodItem mouse = new MethodItem();
                    MethodItem gaze = new MethodItem();

                    if (!rdr.IsDBNull(5))
                    {
                        System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                        mouse._scrolltop.X = s.X;
                        mouse._scrolltop.Y = s.Y;
                        gaze._scrolltop.X = s.X;
                        gaze._scrolltop.Y = s.Y;
                    }
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        mouse._id = rdr.GetInt32(6);
                        mouse._time = rdr.GetInt32(0);
                        mouse._item.X = rdr.GetFloat(1);
                        mouse._item.Y = rdr.GetFloat(2);
                    }
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        gaze._id = rdr.GetInt32(6);
                        gaze._time = rdr.GetInt32(0);
                        gaze._item.X = rdr.GetFloat(3);
                        gaze._item.Y = rdr.GetFloat(4);
                    }

                    #region Mouse
                    //Current point has value
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        //Candidate fixation has points
                        if (Mitems.Count != 0)
                        {
                            int outRangeIndex = -1;
                            for (int i = 0; i < Mitems.Count; i++)
                            {
                                //Web position
                                if ((((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - (mouse._item.X + mouse._scrolltop.X)) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y)) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - (mouse._item.Y + mouse._scrolltop.Y))) > (_spatialThreshold * _spatialThreshold))
                                {
                                    outRangeIndex = i;
                                }
                            }
                            //Distance of current point and certain point in fixation is out of threshold
                            if (outRangeIndex != -1)
                            {
                                //Time of candidate fixation is longer than threshold
                                //Save fixation into DB, Clear candidate fixation to 0 
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                                {
                                    double centerX = Mitems.Average(center => center._item.X);
                                    double centerY = Mitems.Average(center => center._item.Y);
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                        _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                        _sqliteCmd2.ExecuteNonQuery();
                                        mfid++;
                                    }
                                    Mitems.Clear();
                                }
                                //Remove 0 to last outRangeIndex from candidate fixation 
                                else
                                {
                                    Mitems.RemoveRange(0, outRangeIndex + 1);
                                }
                            }
                        }
                        //Add current point in fixation
                        Mitems.Add(mouse);
                    }
                    else
                    {
                        //Candidate fixation has points
                        if (Mitems.Count != 0)
                        {
                            //Time of candidate fixation is longer than threshold
                            //Save fixation into DB
                            if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                            {
                                double centerX = Mitems.Average(center => center._item.X);
                                double centerY = Mitems.Average(center => center._item.Y);
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                    _sqliteCmd2.ExecuteNonQuery();
                                    mfid++;
                                }
                            }
                            //Clear candidate fixation to 0 
                            Mitems.Clear();
                        }
                    }
                    #endregion

                    #region Gaze
                    //Current point has value
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        //Candidate fixation has points
                        if (Gitems.Count != 0)
                        {
                            int outRangeIndex = -1;
                            for (int i = 0; i < Gitems.Count; i++)
                            {
                                if ((((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - (gaze._item.X + gaze._scrolltop.X)) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y)) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - (gaze._item.Y + gaze._scrolltop.Y))) > (_spatialThreshold * _spatialThreshold))
                                {
                                    outRangeIndex = i;
                                }
                            }
                            //Distance of current point and certain point in fixation is out of threshold
                            if (outRangeIndex != -1)
                            {
                                //Time of candidate fixation is longer than threshold
                                //Save fixation into DB, Clear candidate fixation to 0 
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                                {
                                    double centerX = Gitems.Average(center => center._item.X);
                                    double centerY = Gitems.Average(center => center._item.Y);
                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                    {
                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                        _sqliteCmd2.ExecuteNonQuery();
                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                        _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                        _sqliteCmd2.ExecuteNonQuery();
                                        gfid++;
                                    }
                                    Gitems.Clear();
                                }
                                //Remove 0 to last outRangeIndex from candidate fixation 
                                else
                                {
                                    Gitems.RemoveRange(0, outRangeIndex + 1);
                                }
                            }
                        }
                        //Add current point in fixation
                        Gitems.Add(gaze);
                    }
                    else
                    {
                        //Candidate fixation has points
                        if (Gitems.Count != 0)
                        {
                            //Time of candidate fixation is longer than threshold
                            //Save fixation into DB, Clear candidate fixation to 0 
                            if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                            {
                                double centerX = Gitems.Average(center => center._item.X);
                                double centerY = Gitems.Average(center => center._item.Y);
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                    _sqliteCmd2.ExecuteNonQuery();
                                    gfid++;
                                }
                            }
                            Gitems.Clear();
                        }
                    }
                    #endregion

                }

                #region Last_fixation
                //The last fixation
                //Save fixation into DB
                if (Mitems.Count != 0)
                {
                    if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                    {
                        double centerX = Mitems.Average(center => center._item.X);
                        double centerY = Mitems.Average(center => center._item.Y);
                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                            _sqliteCmd2.ExecuteNonQuery();
                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                            _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                            _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                            _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                            _sqliteCmd2.ExecuteNonQuery();
                            mfid++;
                        }
                    }
                }
                Mitems.Clear();

                if (Gitems.Count != 0)
                {
                    if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                    {
                        double centerX = Gitems.Average(center => center._item.X);
                        double centerY = Gitems.Average(center => center._item.Y);
                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                            _sqliteCmd2.ExecuteNonQuery();
                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                            _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                            _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                            _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                            _sqliteCmd2.ExecuteNonQuery();
                            gfid++;
                        }
                    }
                }
                Gitems.Clear();
                #endregion

                rdr.Close();
            }
            return new Tuple<int, int>(mfid, gfid);
        }

        //Each point has a standard deviation of distance from the centroid not exceeding some threshold
        private void Position_Variance_Method()
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    int mfid = 1;
                    int gfid = 1;
                    List<MethodItem> Mitems = new List<MethodItem>();
                    List<MethodItem> Gitems = new List<MethodItem>();
                    bool isMFixation = false;
                    bool isGFixation = false;
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata ORDER BY ID";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        MethodItem mouse = new MethodItem();
                        MethodItem gaze = new MethodItem();

                        if (!rdr.IsDBNull(5))
                        {
                            System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                            mouse._scrolltop.X = s.X;
                            mouse._scrolltop.Y = s.Y;
                            gaze._scrolltop.X = s.X;
                            gaze._scrolltop.Y = s.Y;
                        }
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            mouse._id = rdr.GetInt32(6);
                            mouse._time = rdr.GetInt32(0);
                            mouse._item.X = rdr.GetFloat(1);
                            mouse._item.Y = rdr.GetFloat(2);
                        }
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            gaze._id = rdr.GetInt32(6);
                            gaze._time = rdr.GetInt32(0);
                            gaze._item.X = rdr.GetFloat(3);
                            gaze._item.Y = rdr.GetFloat(4);
                        }

                        #region Mouse
                        //Current point has value
                        if (!rdr.IsDBNull(5) && !rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                //Time of candidate fixation >= threshold
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                                {
                                    if (!isMFixation)
                                    {
                                        //Calculate center and distances to center
                                        double Cx = Mitems.Average(center => center._item.X) + Mitems.Average(center => center._scrolltop.X);
                                        double Cy = Mitems.Average(center => center._item.Y) + Mitems.Average(center => center._scrolltop.Y);
                                        isMFixation = true;
                                        int outRangeIndex = -1;
                                        for (int i = 0; i < Mitems.Count; i++)
                                        {
                                            if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                isMFixation = false;
                                                outRangeIndex = i;
                                            }
                                        }
                                        if (isMFixation)
                                        {
                                            //Calculate center and distances to center
                                            double cx = (Mitems.Sum(center => center._item.X) + Mitems.Sum(center => center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (Mitems.Count + 1);
                                            double cy = (Mitems.Sum(center => center._item.Y) + Mitems.Sum(center => center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (Mitems.Count + 1);
                                            bool expand = true;
                                            for (int i = 0; i < Mitems.Count; i++)
                                            {
                                                if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                                {
                                                    expand = false;
                                                    break;
                                                }
                                            }
                                            if (expand)
                                            {
                                                if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                                {
                                                    expand = false;
                                                }
                                            }
                                            if (!expand)
                                            {
                                                //Save fixation into DB
                                                if (isMFixation)
                                                {
                                                    double centerX = Mitems.Average(center => center._item.X);
                                                    double centerY = Mitems.Average(center => center._item.Y);
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                        _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        mfid++;
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Mitems.Clear();
                                                isMFixation = false;
                                            }

                                        }
                                        else
                                        {
                                            Mitems.RemoveRange(0, outRangeIndex + 1);
                                        }

                                    }
                                    else
                                    {
                                        //Calculate center and distances to center
                                        double cx = (Mitems.Sum(center => center._item.X) + Mitems.Sum(center => center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (Mitems.Count + 1);
                                        double cy = (Mitems.Sum(center => center._item.Y) + Mitems.Sum(center => center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (Mitems.Count + 1);
                                        bool expand = true;
                                        for (int i = 0; i < Mitems.Count; i++)
                                        {
                                            if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                                break;
                                            }
                                        }
                                        if (expand)
                                        {
                                            if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                            }
                                        }
                                        if (!expand)
                                        {
                                            //Save fixation into DB
                                            if (isMFixation)
                                            {
                                                double centerX = Mitems.Average(center => center._item.X);
                                                double centerY = Mitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    mfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Mitems.Clear();
                                            isMFixation = false;
                                        }

                                    }
                                }

                            }
                            //Add current point in fixation
                            Mitems.Add(mouse);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isMFixation)
                            {
                                double centerX = Mitems.Average(center => center._item.X);
                                double centerY = Mitems.Average(center => center._item.Y);
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                    _sqliteCmd2.ExecuteNonQuery();
                                    mfid++;
                                }
                            }
                            //Clear candidate fixation to 0 
                            Mitems.Clear();
                            isMFixation = false;
                        }
                        #endregion

                        #region Gaze
                        //Current point has value
                        if (!rdr.IsDBNull(5) && !rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                //Time of candidate fixation >= threshold
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                                {
                                    if (!isGFixation)
                                    {
                                        //Calculate center and distances to center
                                        double Cx = Gitems.Average(center => center._item.X) + Gitems.Average(center => center._scrolltop.X);
                                        double Cy = Gitems.Average(center => center._item.Y) + Gitems.Average(center => center._scrolltop.Y);
                                        isGFixation = true;
                                        int outRangeIndex = -1;
                                        for (int i = 0; i < Gitems.Count; i++)
                                        {
                                            if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                isGFixation = false;
                                                outRangeIndex = i;
                                            }
                                        }
                                        if (isGFixation)
                                        {
                                            //Calculate center and distances to center
                                            double cx = (Gitems.Sum(center => center._item.X) + Gitems.Sum(center => center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                            double cy = (Gitems.Sum(center => center._item.Y) + Gitems.Sum(center => center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                            bool expand = true;
                                            for (int i = 0; i < Gitems.Count; i++)
                                            {
                                                if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                                {
                                                    expand = false;
                                                    break;
                                                }
                                            }
                                            if (expand)
                                            {
                                                if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                                {
                                                    expand = false;
                                                }
                                            }
                                            if (!expand)
                                            {
                                                //Save fixation into DB
                                                if (isGFixation)
                                                {
                                                    double centerX = Gitems.Average(center => center._item.X);
                                                    double centerY = Gitems.Average(center => center._item.Y);
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                        _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        gfid++;
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Gitems.Clear();
                                                isGFixation = false;
                                            }

                                        }
                                        else
                                        {
                                            Gitems.RemoveRange(0, outRangeIndex + 1);
                                        }

                                    }
                                    else
                                    {
                                        //Calculate center and distances to center
                                        double cx = (Gitems.Sum(center => center._item.X) + Gitems.Sum(center => center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                        double cy = (Gitems.Sum(center => center._item.Y) + Gitems.Sum(center => center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                        bool expand = true;
                                        for (int i = 0; i < Gitems.Count; i++)
                                        {
                                            if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                                break;
                                            }
                                        }
                                        if (expand)
                                        {
                                            if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                            }
                                        }
                                        if (!expand)
                                        {
                                            //Save fixation into DB
                                            if (isGFixation)
                                            {
                                                double centerX = Gitems.Average(center => center._item.X);
                                                double centerY = Gitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    gfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Gitems.Clear();
                                            isGFixation = false;
                                        }

                                    }
                                }

                            }
                            //Add current point in fixation
                            Gitems.Add(gaze);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isGFixation)
                            {
                                double centerX = Gitems.Average(center => center._item.X);
                                double centerY = Gitems.Average(center => center._item.Y);
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                    _sqliteCmd2.ExecuteNonQuery();
                                    gfid++;
                                }
                            }
                            //Clear candidate fixation to 0 
                            Gitems.Clear();
                            isGFixation = false;
                        }
                        #endregion

                    }

                    #region Last_fixation
                    //The last fixation
                    //Save fixation into DB
                    if (isMFixation)
                    {
                        double centerX = Mitems.Average(center => center._item.X);
                        double centerY = Mitems.Average(center => center._item.Y);
                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                            _sqliteCmd2.ExecuteNonQuery();
                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                            _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                            _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                            _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                            _sqliteCmd2.ExecuteNonQuery();
                            mfid++;
                        }
                    }
                    //Clear candidate fixation to 0 
                    Mitems.Clear();
                    isMFixation = false;

                    //Save fixation into DB
                    if (isGFixation)
                    {
                        double centerX = Gitems.Average(center => center._item.X);
                        double centerY = Gitems.Average(center => center._item.Y);
                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                            _sqliteCmd2.ExecuteNonQuery();
                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                            _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                            _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                            _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                            _sqliteCmd2.ExecuteNonQuery();
                            gfid++;
                        }
                    }
                    //Clear candidate fixation to 0 
                    Gitems.Clear();
                    isGFixation = false;
                    #endregion

                    rdr.Close();
                }
                tr.Commit();
            }
        }
        private Tuple<int, int> Position_Variance_Method_WithURL(int startID, int endID, int mfid, int gfid)
        {
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                List<MethodItem> Mitems = new List<MethodItem>();
                List<MethodItem> Gitems = new List<MethodItem>();
                bool isMFixation = false;
                bool isGFixation = false;
                if (endID == -1)
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata WHERE ID>=@startid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                }
                else
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata WHERE ID>=@startid AND ID<@endid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                    _sqliteCmd.Parameters.AddWithValue("@endid", endID);
                }
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    MethodItem mouse = new MethodItem();
                    MethodItem gaze = new MethodItem();

                    if (!rdr.IsDBNull(5))
                    {
                        System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                        mouse._scrolltop.X = s.X;
                        mouse._scrolltop.Y = s.Y;
                        gaze._scrolltop.X = s.X;
                        gaze._scrolltop.Y = s.Y;
                    }
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        mouse._id = rdr.GetInt32(6);
                        mouse._time = rdr.GetInt32(0);
                        mouse._item.X = rdr.GetFloat(1);
                        mouse._item.Y = rdr.GetFloat(2);
                    }
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        gaze._id = rdr.GetInt32(6);
                        gaze._time = rdr.GetInt32(0);
                        gaze._item.X = rdr.GetFloat(3);
                        gaze._item.Y = rdr.GetFloat(4);
                    }

                    #region Mouse
                    //Current point has value
                    if (!rdr.IsDBNull(5) && !rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        //Candidate fixation has points
                        if (Mitems.Count != 0)
                        {
                            //Time of candidate fixation >= threshold
                            if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                            {
                                if (!isMFixation)
                                {
                                    //Calculate center and distances to center
                                    double Cx = Mitems.Average(center => center._item.X) + Mitems.Average(center => center._scrolltop.X);
                                    double Cy = Mitems.Average(center => center._item.Y) + Mitems.Average(center => center._scrolltop.Y);
                                    isMFixation = true;
                                    int outRangeIndex = -1;
                                    for (int i = 0; i < Mitems.Count; i++)
                                    {
                                        if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - Cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - Cy) > _spatialThreshold * _spatialThreshold)
                                        {
                                            isMFixation = false;
                                            outRangeIndex = i;
                                        }
                                    }
                                    if (isMFixation)
                                    {
                                        //Calculate center and distances to center
                                        double cx = (Mitems.Sum(center => center._item.X) + Mitems.Sum(center => center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (Mitems.Count + 1);
                                        double cy = (Mitems.Sum(center => center._item.Y) + Mitems.Sum(center => center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (Mitems.Count + 1);
                                        bool expand = true;
                                        for (int i = 0; i < Mitems.Count; i++)
                                        {
                                            if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                                break;
                                            }
                                        }
                                        if (expand)
                                        {
                                            if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                            }
                                        }
                                        if (!expand)
                                        {
                                            //Save fixation into DB
                                            if (isMFixation)
                                            {
                                                double centerX = Mitems.Average(center => center._item.X);
                                                double centerY = Mitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    mfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Mitems.Clear();
                                            isMFixation = false;
                                        }

                                    }
                                    else
                                    {
                                        Mitems.RemoveRange(0, outRangeIndex + 1);
                                    }

                                }
                                else
                                {
                                    //Calculate center and distances to center
                                    double cx = (Mitems.Sum(center => center._item.X) + Mitems.Sum(center => center._scrolltop.X) + mouse._item.X + mouse._scrolltop.X) / (Mitems.Count + 1);
                                    double cy = (Mitems.Sum(center => center._item.Y) + Mitems.Sum(center => center._scrolltop.Y) + mouse._item.Y + mouse._scrolltop.Y) / (Mitems.Count + 1);
                                    bool expand = true;
                                    for (int i = 0; i < Mitems.Count; i++)
                                    {
                                        if (((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) * ((Mitems[i]._item.X + Mitems[i]._scrolltop.X) - cx) + ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) * ((Mitems[i]._item.Y + Mitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                        {
                                            expand = false;
                                            break;
                                        }
                                    }
                                    if (expand)
                                    {
                                        if (((mouse._item.X + mouse._scrolltop.X) - cx) * ((mouse._item.X + mouse._scrolltop.X) - cx) + ((mouse._item.Y + mouse._scrolltop.Y) - cy) * ((mouse._item.Y + mouse._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                        {
                                            expand = false;
                                        }
                                    }
                                    if (!expand)
                                    {
                                        //Save fixation into DB
                                        if (isMFixation)
                                        {
                                            double centerX = Mitems.Average(center => center._item.X);
                                            double centerY = Mitems.Average(center => center._item.Y);
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                _sqliteCmd2.ExecuteNonQuery();
                                                mfid++;
                                            }
                                        }
                                        //Clear candidate fixation to 0 
                                        Mitems.Clear();
                                        isMFixation = false;
                                    }

                                }
                            }

                        }
                        //Add current point in fixation
                        Mitems.Add(mouse);

                    }
                    else
                    {
                        //IF has fixation, save it into DB
                        if (isMFixation)
                        {
                            double centerX = Mitems.Average(center => center._item.X);
                            double centerY = Mitems.Average(center => center._item.Y);
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                _sqliteCmd2.ExecuteNonQuery();
                                mfid++;
                            }
                        }
                        //Clear candidate fixation to 0 
                        Mitems.Clear();
                        isMFixation = false;
                    }
                    #endregion

                    #region Gaze
                    //Current point has value
                    if (!rdr.IsDBNull(5) && !rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        //Candidate fixation has points
                        if (Gitems.Count != 0)
                        {
                            //Time of candidate fixation >= threshold
                            if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                            {
                                if (!isGFixation)
                                {
                                    //Calculate center and distances to center
                                    double Cx = Gitems.Average(center => center._item.X) + Gitems.Average(center => center._scrolltop.X);
                                    double Cy = Gitems.Average(center => center._item.Y) + Gitems.Average(center => center._scrolltop.Y);
                                    isGFixation = true;
                                    int outRangeIndex = -1;
                                    for (int i = 0; i < Gitems.Count; i++)
                                    {
                                        if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - Cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - Cy) > _spatialThreshold * _spatialThreshold)
                                        {
                                            isGFixation = false;
                                            outRangeIndex = i;
                                        }
                                    }
                                    if (isGFixation)
                                    {
                                        //Calculate center and distances to center
                                        double cx = (Gitems.Sum(center => center._item.X) + Gitems.Sum(center => center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                        double cy = (Gitems.Sum(center => center._item.Y) + Gitems.Sum(center => center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                        bool expand = true;
                                        for (int i = 0; i < Gitems.Count; i++)
                                        {
                                            if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                                break;
                                            }
                                        }
                                        if (expand)
                                        {
                                            if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                            {
                                                expand = false;
                                            }
                                        }
                                        if (!expand)
                                        {
                                            //Save fixation into DB
                                            if (isGFixation)
                                            {
                                                double centerX = Gitems.Average(center => center._item.X);
                                                double centerY = Gitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    gfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Gitems.Clear();
                                            isGFixation = false;
                                        }

                                    }
                                    else
                                    {
                                        Gitems.RemoveRange(0, outRangeIndex + 1);
                                    }

                                }
                                else
                                {
                                    //Calculate center and distances to center
                                    double cx = (Gitems.Sum(center => center._item.X) + Gitems.Sum(center => center._scrolltop.X) + gaze._item.X + gaze._scrolltop.X) / (Gitems.Count + 1);
                                    double cy = (Gitems.Sum(center => center._item.Y) + Gitems.Sum(center => center._scrolltop.Y) + gaze._item.Y + gaze._scrolltop.Y) / (Gitems.Count + 1);
                                    bool expand = true;
                                    for (int i = 0; i < Gitems.Count; i++)
                                    {
                                        if (((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) * ((Gitems[i]._item.X + Gitems[i]._scrolltop.X) - cx) + ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) * ((Gitems[i]._item.Y + Gitems[i]._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                        {
                                            expand = false;
                                            break;
                                        }
                                    }
                                    if (expand)
                                    {
                                        if (((gaze._item.X + gaze._scrolltop.X) - cx) * ((gaze._item.X + gaze._scrolltop.X) - cx) + ((gaze._item.Y + gaze._scrolltop.Y) - cy) * ((gaze._item.Y + gaze._scrolltop.Y) - cy) > _spatialThreshold * _spatialThreshold)
                                        {
                                            expand = false;
                                        }
                                    }
                                    if (!expand)
                                    {
                                        //Save fixation into DB
                                        if (isGFixation)
                                        {
                                            double centerX = Gitems.Average(center => center._item.X);
                                            double centerY = Gitems.Average(center => center._item.Y);
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                _sqliteCmd2.ExecuteNonQuery();
                                                gfid++;
                                            }
                                        }
                                        //Clear candidate fixation to 0 
                                        Gitems.Clear();
                                        isGFixation = false;
                                    }

                                }
                            }

                        }
                        //Add current point in fixation
                        Gitems.Add(gaze);

                    }
                    else
                    {
                        //IF has fixation, save it into DB
                        if (isGFixation)
                        {
                            double centerX = Gitems.Average(center => center._item.X);
                            double centerY = Gitems.Average(center => center._item.Y);
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                _sqliteCmd2.ExecuteNonQuery();
                                gfid++;
                            }
                        }
                        //Clear candidate fixation to 0 
                        Gitems.Clear();
                        isGFixation = false;
                    }
                    #endregion

                }

                #region Last_fixation
                //The last fixation
                //Save fixation into DB
                if (isMFixation)
                {
                    double centerX = Mitems.Average(center => center._item.X);
                    double centerY = Mitems.Average(center => center._item.Y);
                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                        _sqliteCmd2.ExecuteNonQuery();
                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                        _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                        _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                        _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                        _sqliteCmd2.ExecuteNonQuery();
                        mfid++;
                    }
                }
                //Clear candidate fixation to 0 
                Mitems.Clear();
                isMFixation = false;

                //Save fixation into DB
                if (isGFixation)
                {
                    double centerX = Gitems.Average(center => center._item.X);
                    double centerY = Gitems.Average(center => center._item.Y);
                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                        _sqliteCmd2.ExecuteNonQuery();
                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                        _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                        _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                        _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                        _sqliteCmd2.ExecuteNonQuery();
                        gfid++;
                    }
                }
                //Clear candidate fixation to 0 
                Gitems.Clear();
                isGFixation = false;
                #endregion

                rdr.Close();
            }
            return new Tuple<int, int>(mfid, gfid);
        }

        //The maximal horizontal distance plus the maximal vertical distance is less than some threshold
        private void Salvucci_IDT_Method()
        {
            using (SQLiteTransaction tr = _sqliteConnect.BeginTransaction())
            {
                using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
                {
                    int mfid = 1;
                    int gfid = 1;
                    List<MethodItem> Mitems = new List<MethodItem>();
                    List<MethodItem> Gitems = new List<MethodItem>();
                    bool isMFixation = false;
                    bool isGFixation = false;
                    double[,] dispersionM = new double[5, 2]; // {{x_distance, y_distance},{index, x_min},{index, x_max},{index, y_min},{index, y_max}}
                    double[,] dispersionG = new double[5, 2]; // {{x_distance, y_distance},{index, x_min},{index, x_max},{index, y_min},{index, y_max}}
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata ORDER BY ID";
                    var rdr = _sqliteCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        MethodItem mouse = new MethodItem();
                        MethodItem gaze = new MethodItem();

                        if (!rdr.IsDBNull(5))
                        {
                            System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                            mouse._scrolltop.X = s.X;
                            mouse._scrolltop.Y = s.Y;
                            gaze._scrolltop.X = s.X;
                            gaze._scrolltop.Y = s.Y;
                        }
                        if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            mouse._id = rdr.GetInt32(6);
                            mouse._time = rdr.GetInt32(0);
                            mouse._item.X = rdr.GetFloat(1);
                            mouse._item.Y = rdr.GetFloat(2);
                        }
                        if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            gaze._id = rdr.GetInt32(6);
                            gaze._time = rdr.GetInt32(0);
                            gaze._item.X = rdr.GetFloat(3);
                            gaze._item.Y = rdr.GetFloat(4);
                        }

                        #region Mouse
                        //Current point has value
                        if (!rdr.IsDBNull(5) && !rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                        {
                            //Candidate fixation has points
                            if (Mitems.Count != 0)
                            {
                                //Time of candidate fixation >= threshold
                                if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                                {
                                    if (!isMFixation)
                                    {
                                        //Calculate dispersion
                                        for (int i = 0; i < Mitems.Count; i++)
                                        {
                                            if (i == 0)
                                            {
                                                dispersionM[1, 0] = 0;
                                                dispersionM[2, 0] = 0;
                                                dispersionM[3, 0] = 0;
                                                dispersionM[4, 0] = 0;
                                                dispersionM[1, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; //x_min
                                                dispersionM[2, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; //x_max
                                                dispersionM[3, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; //y_min
                                                dispersionM[4, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; //y_max
                                                dispersionM[0, 0] = 0;
                                                dispersionM[0, 1] = 0;
                                            }
                                            else
                                            {
                                                if (Mitems[i]._item.X + Mitems[i]._scrolltop.X <= dispersionM[1, 1]) { dispersionM[1, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; dispersionM[1, 0] = i; dispersionM[0, 0] = dispersionM[2, 1] - dispersionM[1, 1]; }
                                                if (Mitems[i]._item.X + Mitems[i]._scrolltop.X >= dispersionM[2, 1]) { dispersionM[2, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; dispersionM[2, 0] = i; dispersionM[0, 0] = dispersionM[2, 1] - dispersionM[1, 1]; }
                                                if (Mitems[i]._item.Y + Mitems[i]._scrolltop.Y <= dispersionM[3, 1]) { dispersionM[3, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; dispersionM[3, 0] = i; dispersionM[0, 1] = dispersionM[4, 1] - dispersionM[3, 1]; }
                                                if (Mitems[i]._item.Y + Mitems[i]._scrolltop.Y >= dispersionM[4, 1]) { dispersionM[4, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; dispersionM[4, 0] = i; dispersionM[0, 1] = dispersionM[4, 1] - dispersionM[3, 1]; }
                                            }
                                        }
                                        if (dispersionM[0, 0] + dispersionM[0, 1] <= _spatialThreshold)
                                        {
                                            isMFixation = true;
                                            double[,] tmp = dispersionM;
                                            if (mouse._item.X + mouse._scrolltop.X <= dispersionM[1, 1]) { tmp[1, 1] = mouse._item.X + mouse._scrolltop.X; tmp[1, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                            if (mouse._item.X + mouse._scrolltop.X >= dispersionM[2, 1]) { tmp[2, 1] = mouse._item.X + mouse._scrolltop.X; tmp[2, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                            if (mouse._item.Y + mouse._scrolltop.Y <= dispersionM[3, 1]) { tmp[3, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[3, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                            if (mouse._item.Y + mouse._scrolltop.Y >= dispersionM[4, 1]) { tmp[4, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[4, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                            if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                            {
                                                dispersionM = tmp;
                                            }
                                            else
                                            {
                                                //Save fixation into DB
                                                if (isMFixation)
                                                {
                                                    double centerX = Mitems.Average(center => center._item.X);
                                                    double centerY = Mitems.Average(center => center._item.Y);
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                        _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        mfid++;
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Mitems.Clear();
                                                isMFixation = false;
                                            }
                                        }
                                        else
                                        {
                                            //remove 0 - first of 4 point, add point
                                            double removeIndex = dispersionM[1, 0];
                                            if (dispersionM[2, 0] > removeIndex) { removeIndex = dispersionM[2, 0]; }
                                            if (dispersionM[3, 0] > removeIndex) { removeIndex = dispersionM[3, 0]; }
                                            if (dispersionM[4, 0] > removeIndex) { removeIndex = dispersionM[4, 0]; }
                                            Mitems.RemoveRange(0, (int)removeIndex + 1);
                                        }
                                    }
                                    else
                                    {
                                        double[,] tmp = dispersionM;
                                        if (mouse._item.X + mouse._scrolltop.X <= dispersionM[1, 1]) { tmp[1, 1] = mouse._item.X + mouse._scrolltop.X; tmp[1, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (mouse._item.X + mouse._scrolltop.X >= dispersionM[2, 1]) { tmp[2, 1] = mouse._item.X + mouse._scrolltop.X; tmp[2, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (mouse._item.Y + mouse._scrolltop.Y <= dispersionM[3, 1]) { tmp[3, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[3, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (mouse._item.Y + mouse._scrolltop.Y >= dispersionM[4, 1]) { tmp[4, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[4, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                        {
                                            dispersionM = tmp;
                                        }
                                        else
                                        {
                                            //Save fixation into DB
                                            if (isMFixation)
                                            {
                                                double centerX = Mitems.Average(center => center._item.X);
                                                double centerY = Mitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    mfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Mitems.Clear();
                                            isMFixation = false;
                                        }

                                    }
                                }

                            }
                            //Add current point in fixation
                            Mitems.Add(mouse);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isMFixation)
                            {
                                double centerX = Mitems.Average(center => center._item.X);
                                double centerY = Mitems.Average(center => center._item.Y);
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                    _sqliteCmd2.ExecuteNonQuery();
                                    mfid++;
                                }
                            }
                            //Clear candidate fixation to 0 
                            Mitems.Clear();
                            isMFixation = false;
                        }
                        #endregion

                        #region Gaze
                        //Current point has value
                        if (!rdr.IsDBNull(5) && !rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                        {
                            //Candidate fixation has points
                            if (Gitems.Count != 0)
                            {
                                //Time of candidate fixation >= threshold
                                if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                                {
                                    if (!isGFixation)
                                    {
                                        //Calculate dispersion
                                        for (int i = 0; i < Gitems.Count; i++)
                                        {
                                            if (i == 0)
                                            {
                                                dispersionG[1, 0] = 0;
                                                dispersionG[2, 0] = 0;
                                                dispersionG[3, 0] = 0;
                                                dispersionG[4, 0] = 0;
                                                dispersionG[1, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; //x_min
                                                dispersionG[2, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; //x_max
                                                dispersionG[3, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; //y_min
                                                dispersionG[4, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; //y_max
                                                dispersionG[0, 0] = 0;
                                                dispersionG[0, 1] = 0;
                                            }
                                            else
                                            {
                                                if (Gitems[i]._item.X + Gitems[i]._scrolltop.X <= dispersionG[1, 1]) { dispersionG[1, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; dispersionG[1, 0] = i; dispersionG[0, 0] = dispersionG[2, 1] - dispersionG[1, 1]; }
                                                if (Gitems[i]._item.X + Gitems[i]._scrolltop.X >= dispersionG[2, 1]) { dispersionG[2, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; dispersionG[2, 0] = i; dispersionG[0, 0] = dispersionG[2, 1] - dispersionG[1, 1]; }
                                                if (Gitems[i]._item.Y + Gitems[i]._scrolltop.Y <= dispersionG[3, 1]) { dispersionG[3, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; dispersionG[3, 0] = i; dispersionG[0, 1] = dispersionG[4, 1] - dispersionG[3, 1]; }
                                                if (Gitems[i]._item.Y + Gitems[i]._scrolltop.Y >= dispersionG[4, 1]) { dispersionG[4, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; dispersionG[4, 0] = i; dispersionG[0, 1] = dispersionG[4, 1] - dispersionG[3, 1]; }
                                            }
                                        }
                                        if (dispersionG[0, 0] + dispersionG[0, 1] <= _spatialThreshold)
                                        {
                                            isGFixation = true;
                                            double[,] tmp = dispersionG;
                                            if (gaze._item.X + gaze._scrolltop.X <= dispersionG[1, 1]) { tmp[1, 1] = gaze._item.X + gaze._scrolltop.X; tmp[1, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                            if (gaze._item.X + gaze._scrolltop.X >= dispersionG[2, 1]) { tmp[2, 1] = gaze._item.X + gaze._scrolltop.X; tmp[2, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                            if (gaze._item.Y + gaze._scrolltop.Y <= dispersionG[3, 1]) { tmp[3, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[3, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                            if (gaze._item.Y + gaze._scrolltop.Y >= dispersionG[4, 1]) { tmp[4, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[4, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                            if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                            {
                                                dispersionG = tmp;
                                            }
                                            else
                                            {
                                                //Save fixation into DB
                                                if (isGFixation)
                                                {
                                                    double centerX = Gitems.Average(center => center._item.X);
                                                    double centerY = Gitems.Average(center => center._item.Y);
                                                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                    {
                                                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                        _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                        _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                        _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                        _sqliteCmd2.ExecuteNonQuery();
                                                        gfid++;
                                                    }
                                                }
                                                //Clear candidate fixation to 0 
                                                Gitems.Clear();
                                                isGFixation = false;
                                            }
                                        }
                                        else
                                        {
                                            //remove 0 - first of 4 point, add point
                                            double removeIndex = dispersionG[1, 0];
                                            if (dispersionG[2, 0] > removeIndex) { removeIndex = dispersionG[2, 0]; }
                                            if (dispersionG[3, 0] > removeIndex) { removeIndex = dispersionG[3, 0]; }
                                            if (dispersionG[4, 0] > removeIndex) { removeIndex = dispersionG[4, 0]; }
                                            Gitems.RemoveRange(0, (int)removeIndex + 1);
                                        }
                                    }
                                    else
                                    {
                                        double[,] tmp = dispersionG;
                                        if (gaze._item.X + gaze._scrolltop.X <= dispersionG[1, 1]) { tmp[1, 1] = gaze._item.X + gaze._scrolltop.X; tmp[1, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (gaze._item.X + gaze._scrolltop.X >= dispersionG[2, 1]) { tmp[2, 1] = gaze._item.X + gaze._scrolltop.X; tmp[2, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (gaze._item.Y + gaze._scrolltop.Y <= dispersionG[3, 1]) { tmp[3, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[3, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (gaze._item.Y + gaze._scrolltop.Y >= dispersionG[4, 1]) { tmp[4, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[4, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                        {
                                            dispersionG = tmp;
                                        }
                                        else
                                        {
                                            //Save fixation into DB
                                            if (isGFixation)
                                            {
                                                double centerX = Gitems.Average(center => center._item.X);
                                                double centerY = Gitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    gfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Gitems.Clear();
                                            isGFixation = false;
                                        }

                                    }
                                }

                            }
                            //Add current point in fixation
                            Gitems.Add(gaze);

                        }
                        else
                        {
                            //IF has fixation, save it into DB
                            if (isGFixation)
                            {
                                double centerX = Gitems.Average(center => center._item.X);
                                double centerY = Gitems.Average(center => center._item.Y);
                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                {
                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                    _sqliteCmd2.ExecuteNonQuery();
                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                    _sqliteCmd2.ExecuteNonQuery();
                                    gfid++;
                                }
                            }
                            //Clear candidate fixation to 0 
                            Gitems.Clear();
                            isGFixation = false;
                        }
                        #endregion

                    }

                    #region Last_fixation
                    //The last fixation
                    //Save fixation into DB
                    if (isMFixation)
                    {
                        double centerX = Mitems.Average(center => center._item.X);
                        double centerY = Mitems.Average(center => center._item.Y);
                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                            _sqliteCmd2.ExecuteNonQuery();
                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                            _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                            _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                            _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                            _sqliteCmd2.ExecuteNonQuery();
                            mfid++;
                        }
                    }
                    //Clear candidate fixation to 0 
                    Mitems.Clear();
                    isMFixation = false;

                    //Save fixation into DB
                    if (isGFixation)
                    {
                        double centerX = Gitems.Average(center => center._item.X);
                        double centerY = Gitems.Average(center => center._item.Y);
                        using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                        {
                            _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                            _sqliteCmd2.ExecuteNonQuery();
                            _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                            _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                            _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                            _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                            _sqliteCmd2.ExecuteNonQuery();
                            gfid++;
                        }
                    }
                    //Clear candidate fixation to 0 
                    Gitems.Clear();
                    isGFixation = false;
                    #endregion

                    rdr.Close();
                }
                tr.Commit();
            }
        }
        private Tuple<int, int> Salvucci_IDT_Method_WithURL(int startID, int endID, int mfid, int gfid)
        {
            using (SQLiteCommand _sqliteCmd = _sqliteConnect.CreateCommand())
            {
                List<MethodItem> Mitems = new List<MethodItem>();
                List<MethodItem> Gitems = new List<MethodItem>();
                bool isMFixation = false;
                bool isGFixation = false;
                double[,] dispersionM = new double[5, 2]; // {{x_distance, y_distance},{index, x_min},{index, x_max},{index, y_min},{index, y_max}}
                double[,] dispersionG = new double[5, 2]; // {{x_distance, y_distance},{index, x_min},{index, x_max},{index, y_min},{index, y_max}}
                if (endID == -1)
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata ID>=@startid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                }
                else
                {
                    _sqliteCmd.CommandText = "SELECT Time, MousePosX, MousePosY, GazePosX, GazePosY, ScrollTop, ID FROM " + _SubjectName + "Rawdata ID>=@startid AND ID<@endid ORDER BY ID";
                    _sqliteCmd.Parameters.AddWithValue("@startid", startID);
                    _sqliteCmd.Parameters.AddWithValue("@endid", endID);
                }
                var rdr = _sqliteCmd.ExecuteReader();
                while (rdr.Read())
                {
                    MethodItem mouse = new MethodItem();
                    MethodItem gaze = new MethodItem();

                    if (!rdr.IsDBNull(5))
                    {
                        System.Windows.Point s = System.Windows.Point.Parse(rdr.GetString(5));
                        mouse._scrolltop.X = s.X;
                        mouse._scrolltop.Y = s.Y;
                        gaze._scrolltop.X = s.X;
                        gaze._scrolltop.Y = s.Y;
                    }
                    if (!rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        mouse._id = rdr.GetInt32(6);
                        mouse._time = rdr.GetInt32(0);
                        mouse._item.X = rdr.GetFloat(1);
                        mouse._item.Y = rdr.GetFloat(2);
                    }
                    if (!rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        gaze._id = rdr.GetInt32(6);
                        gaze._time = rdr.GetInt32(0);
                        gaze._item.X = rdr.GetFloat(3);
                        gaze._item.Y = rdr.GetFloat(4);
                    }

                    #region Mouse
                    //Current point has value
                    if (!rdr.IsDBNull(5) && !rdr.IsDBNull(1) && !rdr.IsDBNull(2))
                    {
                        //Candidate fixation has points
                        if (Mitems.Count != 0)
                        {
                            //Time of candidate fixation >= threshold
                            if ((Mitems[Mitems.Count - 1]._time - Mitems[0]._time) >= _durationThreshold)
                            {
                                if (!isMFixation)
                                {
                                    //Calculate dispersion
                                    for (int i = 0; i < Mitems.Count; i++)
                                    {
                                        if (i == 0)
                                        {
                                            dispersionM[1, 0] = 0;
                                            dispersionM[2, 0] = 0;
                                            dispersionM[3, 0] = 0;
                                            dispersionM[4, 0] = 0;
                                            dispersionM[1, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; //x_min
                                            dispersionM[2, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; //x_max
                                            dispersionM[3, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; //y_min
                                            dispersionM[4, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; //y_max
                                            dispersionM[0, 0] = 0;
                                            dispersionM[0, 1] = 0;
                                        }
                                        else
                                        {
                                            if (Mitems[i]._item.X + Mitems[i]._scrolltop.X <= dispersionM[1, 1]) { dispersionM[1, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; dispersionM[1, 0] = i; dispersionM[0, 0] = dispersionM[2, 1] - dispersionM[1, 1]; }
                                            if (Mitems[i]._item.X + Mitems[i]._scrolltop.X >= dispersionM[2, 1]) { dispersionM[2, 1] = Mitems[i]._item.X + Mitems[i]._scrolltop.X; dispersionM[2, 0] = i; dispersionM[0, 0] = dispersionM[2, 1] - dispersionM[1, 1]; }
                                            if (Mitems[i]._item.Y + Mitems[i]._scrolltop.Y <= dispersionM[3, 1]) { dispersionM[3, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; dispersionM[3, 0] = i; dispersionM[0, 1] = dispersionM[4, 1] - dispersionM[3, 1]; }
                                            if (Mitems[i]._item.Y + Mitems[i]._scrolltop.Y >= dispersionM[4, 1]) { dispersionM[4, 1] = Mitems[i]._item.Y + Mitems[i]._scrolltop.Y; dispersionM[4, 0] = i; dispersionM[0, 1] = dispersionM[4, 1] - dispersionM[3, 1]; }
                                        }
                                    }
                                    if (dispersionM[0, 0] + dispersionM[0, 1] <= _spatialThreshold)
                                    {
                                        isMFixation = true;
                                        double[,] tmp = dispersionM;
                                        if (mouse._item.X + mouse._scrolltop.X <= dispersionM[1, 1]) { tmp[1, 1] = mouse._item.X + mouse._scrolltop.X; tmp[1, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (mouse._item.X + mouse._scrolltop.X >= dispersionM[2, 1]) { tmp[2, 1] = mouse._item.X + mouse._scrolltop.X; tmp[2, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (mouse._item.Y + mouse._scrolltop.Y <= dispersionM[3, 1]) { tmp[3, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[3, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (mouse._item.Y + mouse._scrolltop.Y >= dispersionM[4, 1]) { tmp[4, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[4, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                        {
                                            dispersionM = tmp;
                                        }
                                        else
                                        {
                                            //Save fixation into DB
                                            if (isMFixation)
                                            {
                                                double centerX = Mitems.Average(center => center._item.X);
                                                double centerY = Mitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    mfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Mitems.Clear();
                                            isMFixation = false;
                                        }
                                    }
                                    else
                                    {
                                        //remove 0 - first of 4 point, add point
                                        double removeIndex = dispersionM[1, 0];
                                        if (dispersionM[2, 0] > removeIndex) { removeIndex = dispersionM[2, 0]; }
                                        if (dispersionM[3, 0] > removeIndex) { removeIndex = dispersionM[3, 0]; }
                                        if (dispersionM[4, 0] > removeIndex) { removeIndex = dispersionM[4, 0]; }
                                        Mitems.RemoveRange(0, (int)removeIndex + 1);
                                    }
                                }
                                else
                                {
                                    double[,] tmp = dispersionM;
                                    if (mouse._item.X + mouse._scrolltop.X <= dispersionM[1, 1]) { tmp[1, 1] = mouse._item.X + mouse._scrolltop.X; tmp[1, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                    if (mouse._item.X + mouse._scrolltop.X >= dispersionM[2, 1]) { tmp[2, 1] = mouse._item.X + mouse._scrolltop.X; tmp[2, 0] = Mitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                    if (mouse._item.Y + mouse._scrolltop.Y <= dispersionM[3, 1]) { tmp[3, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[3, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                    if (mouse._item.Y + mouse._scrolltop.Y >= dispersionM[4, 1]) { tmp[4, 1] = mouse._item.Y + mouse._scrolltop.Y; tmp[4, 0] = Mitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                    if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                    {
                                        dispersionM = tmp;
                                    }
                                    else
                                    {
                                        //Save fixation into DB
                                        if (isMFixation)
                                        {
                                            double centerX = Mitems.Average(center => center._item.X);
                                            double centerY = Mitems.Average(center => center._item.Y);
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                                _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                                _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                                _sqliteCmd2.ExecuteNonQuery();
                                                mfid++;
                                            }
                                        }
                                        //Clear candidate fixation to 0 
                                        Mitems.Clear();
                                        isMFixation = false;
                                    }

                                }
                            }

                        }
                        //Add current point in fixation
                        Mitems.Add(mouse);

                    }
                    else
                    {
                        //IF has fixation, save it into DB
                        if (isMFixation)
                        {
                            double centerX = Mitems.Average(center => center._item.X);
                            double centerY = Mitems.Average(center => center._item.Y);
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                                _sqliteCmd2.ExecuteNonQuery();
                                mfid++;
                            }
                        }
                        //Clear candidate fixation to 0 
                        Mitems.Clear();
                        isMFixation = false;
                    }
                    #endregion

                    #region Gaze
                    //Current point has value
                    if (!rdr.IsDBNull(5) && !rdr.IsDBNull(3) && !rdr.IsDBNull(4))
                    {
                        //Candidate fixation has points
                        if (Gitems.Count != 0)
                        {
                            //Time of candidate fixation >= threshold
                            if ((Gitems[Gitems.Count - 1]._time - Gitems[0]._time) >= _durationThreshold)
                            {
                                if (!isGFixation)
                                {
                                    //Calculate dispersion
                                    for (int i = 0; i < Gitems.Count; i++)
                                    {
                                        if (i == 0)
                                        {
                                            dispersionG[1, 0] = 0;
                                            dispersionG[2, 0] = 0;
                                            dispersionG[3, 0] = 0;
                                            dispersionG[4, 0] = 0;
                                            dispersionG[1, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; //x_min
                                            dispersionG[2, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; //x_max
                                            dispersionG[3, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; //y_min
                                            dispersionG[4, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; //y_max
                                            dispersionG[0, 0] = 0;
                                            dispersionG[0, 1] = 0;
                                        }
                                        else
                                        {
                                            if (Gitems[i]._item.X + Gitems[i]._scrolltop.X <= dispersionG[1, 1]) { dispersionG[1, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; dispersionG[1, 0] = i; dispersionG[0, 0] = dispersionG[2, 1] - dispersionG[1, 1]; }
                                            if (Gitems[i]._item.X + Gitems[i]._scrolltop.X >= dispersionG[2, 1]) { dispersionG[2, 1] = Gitems[i]._item.X + Gitems[i]._scrolltop.X; dispersionG[2, 0] = i; dispersionG[0, 0] = dispersionG[2, 1] - dispersionG[1, 1]; }
                                            if (Gitems[i]._item.Y + Gitems[i]._scrolltop.Y <= dispersionG[3, 1]) { dispersionG[3, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; dispersionG[3, 0] = i; dispersionG[0, 1] = dispersionG[4, 1] - dispersionG[3, 1]; }
                                            if (Gitems[i]._item.Y + Gitems[i]._scrolltop.Y >= dispersionG[4, 1]) { dispersionG[4, 1] = Gitems[i]._item.Y + Gitems[i]._scrolltop.Y; dispersionG[4, 0] = i; dispersionG[0, 1] = dispersionG[4, 1] - dispersionG[3, 1]; }
                                        }
                                    }
                                    if (dispersionG[0, 0] + dispersionG[0, 1] <= _spatialThreshold)
                                    {
                                        isGFixation = true;
                                        double[,] tmp = dispersionG;
                                        if (gaze._item.X + gaze._scrolltop.X <= dispersionG[1, 1]) { tmp[1, 1] = gaze._item.X + gaze._scrolltop.X; tmp[1, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (gaze._item.X + gaze._scrolltop.X >= dispersionG[2, 1]) { tmp[2, 1] = gaze._item.X + gaze._scrolltop.X; tmp[2, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                        if (gaze._item.Y + gaze._scrolltop.Y <= dispersionG[3, 1]) { tmp[3, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[3, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (gaze._item.Y + gaze._scrolltop.Y >= dispersionG[4, 1]) { tmp[4, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[4, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                        if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                        {
                                            dispersionG = tmp;
                                        }
                                        else
                                        {
                                            //Save fixation into DB
                                            if (isGFixation)
                                            {
                                                double centerX = Gitems.Average(center => center._item.X);
                                                double centerY = Gitems.Average(center => center._item.Y);
                                                using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                                {
                                                    _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                    _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                    _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                    _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                    _sqliteCmd2.ExecuteNonQuery();
                                                    gfid++;
                                                }
                                            }
                                            //Clear candidate fixation to 0 
                                            Gitems.Clear();
                                            isGFixation = false;
                                        }
                                    }
                                    else
                                    {
                                        //remove 0 - first of 4 point, add point
                                        double removeIndex = dispersionG[1, 0];
                                        if (dispersionG[2, 0] > removeIndex) { removeIndex = dispersionG[2, 0]; }
                                        if (dispersionG[3, 0] > removeIndex) { removeIndex = dispersionG[3, 0]; }
                                        if (dispersionG[4, 0] > removeIndex) { removeIndex = dispersionG[4, 0]; }
                                        Gitems.RemoveRange(0, (int)removeIndex + 1);
                                    }
                                }
                                else
                                {
                                    double[,] tmp = dispersionG;
                                    if (gaze._item.X + gaze._scrolltop.X <= dispersionG[1, 1]) { tmp[1, 1] = gaze._item.X + gaze._scrolltop.X; tmp[1, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                    if (gaze._item.X + gaze._scrolltop.X >= dispersionG[2, 1]) { tmp[2, 1] = gaze._item.X + gaze._scrolltop.X; tmp[2, 0] = Gitems.Count; tmp[0, 0] = tmp[2, 1] - tmp[1, 1]; }
                                    if (gaze._item.Y + gaze._scrolltop.Y <= dispersionG[3, 1]) { tmp[3, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[3, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                    if (gaze._item.Y + gaze._scrolltop.Y >= dispersionG[4, 1]) { tmp[4, 1] = gaze._item.Y + gaze._scrolltop.Y; tmp[4, 0] = Gitems.Count; tmp[0, 1] = tmp[4, 1] - tmp[3, 1]; }
                                    if (tmp[0, 0] + tmp[0, 1] <= _spatialThreshold)
                                    {
                                        dispersionG = tmp;
                                    }
                                    else
                                    {
                                        //Save fixation into DB
                                        if (isGFixation)
                                        {
                                            double centerX = Gitems.Average(center => center._item.X);
                                            double centerY = Gitems.Average(center => center._item.Y);
                                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                                            {
                                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                                _sqliteCmd2.ExecuteNonQuery();
                                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                                _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                                _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                                _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                                _sqliteCmd2.ExecuteNonQuery();
                                                gfid++;
                                            }
                                        }
                                        //Clear candidate fixation to 0 
                                        Gitems.Clear();
                                        isGFixation = false;
                                    }

                                }
                            }

                        }
                        //Add current point in fixation
                        Gitems.Add(gaze);

                    }
                    else
                    {
                        //IF has fixation, save it into DB
                        if (isGFixation)
                        {
                            double centerX = Gitems.Average(center => center._item.X);
                            double centerY = Gitems.Average(center => center._item.Y);
                            using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                            {
                                _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                                _sqliteCmd2.ExecuteNonQuery();
                                _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                                _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                                _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                                _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                                _sqliteCmd2.ExecuteNonQuery();
                                gfid++;
                            }
                        }
                        //Clear candidate fixation to 0 
                        Gitems.Clear();
                        isGFixation = false;
                    }
                    #endregion

                }

                #region Last_fixation
                //The last fixation
                //Save fixation into DB
                if (isMFixation)
                {
                    double centerX = Mitems.Average(center => center._item.X);
                    double centerY = Mitems.Average(center => center._item.Y);
                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd2.CommandText = "INSERT INTO MouseFixation VALUES (null, '" + _SubjectName + "', '" + Mitems[0]._time + "', '" + (Mitems[Mitems.Count - 1]._time - Mitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Mitems[0]._scrolltop.X + "', '" + Mitems[0]._scrolltop.Y + "', '" + Mitems[0]._id + "', '" + Mitems[Mitems.Count - 1]._id + "', '" + mfid + "')";
                        _sqliteCmd2.ExecuteNonQuery();
                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET MFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                        _sqliteCmd2.Parameters.AddWithValue("@fid", mfid);
                        _sqliteCmd2.Parameters.AddWithValue("@startid", Mitems[0]._id);
                        _sqliteCmd2.Parameters.AddWithValue("@endid", Mitems[Mitems.Count - 1]._id);
                        _sqliteCmd2.ExecuteNonQuery();
                        mfid++;
                    }
                }
                //Clear candidate fixation to 0 
                Mitems.Clear();
                isMFixation = false;

                //Save fixation into DB
                if (isGFixation)
                {
                    double centerX = Gitems.Average(center => center._item.X);
                    double centerY = Gitems.Average(center => center._item.Y);
                    using (SQLiteCommand _sqliteCmd2 = _sqliteConnect.CreateCommand())
                    {
                        _sqliteCmd2.CommandText = "INSERT INTO GazeFixation VALUES (null, '" + _SubjectName + "', '" + Gitems[0]._time + "', '" + (Gitems[Gitems.Count - 1]._time - Gitems[0]._time) + "', '" + centerX + "', '" + centerY + "', '" + Gitems[0]._scrolltop.X + "', '" + Gitems[0]._scrolltop.Y + "', '" + Gitems[0]._id + "', '" + Gitems[Gitems.Count - 1]._id + "', '" + gfid + "')";
                        _sqliteCmd2.ExecuteNonQuery();
                        _sqliteCmd2.CommandText = "UPDATE " + _SubjectName + "Rawdata SET GFixationID=@fid WHERE ID>=@startid AND ID<=@endid";
                        _sqliteCmd2.Parameters.AddWithValue("@fid", gfid);
                        _sqliteCmd2.Parameters.AddWithValue("@startid", Gitems[0]._id);
                        _sqliteCmd2.Parameters.AddWithValue("@endid", Gitems[Gitems.Count - 1]._id);
                        _sqliteCmd2.ExecuteNonQuery();
                        gfid++;
                    }
                }
                //Clear candidate fixation to 0 
                Gitems.Clear();
                isGFixation = false;
                #endregion

                rdr.Close();
            }
            return new Tuple<int, int>(mfid, gfid);
        }

    }
    class MethodItem
    {
        public int _id;
        public int _time;
        public System.Windows.Point _item;
        public System.Windows.Point _scrolltop;
        public MethodItem()
        {
            _id = -1;
            _time = -1;
            _item = new System.Windows.Point(-1, -1);
            _scrolltop = new System.Windows.Point(0, 0);
        }
    }
    #endregion
}
