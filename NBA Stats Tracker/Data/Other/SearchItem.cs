﻿#region Copyright Notice

//     Copyright 2011-2013 Eleftherios Aslanoglou
//  
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
//  
//         http:www.apache.org/licenses/LICENSE-2.0
//  
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.

#endregion

namespace NBA_Stats_Tracker.Data.Other
{
    public class SearchItem
    {
        #region SelectionType enum

        public enum SelectionType
        {
            Team,
            Player
        };

        #endregion

        public string Description;
        public int ID;
        public SelectionType Type;

        public SearchItem()
        {
        }

        public SearchItem(SelectionType type, int id, string description)
        {
            Type = type;
            ID = id;
            Description = description;
        }

        public override string ToString()
        {
            return string.Format("({0}) {1}", Type == SelectionType.Team ? "Team" : "Player", Description);
        }
    }
}