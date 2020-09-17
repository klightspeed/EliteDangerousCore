/*
 * Copyright © 2016 - 2018 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using EliteDangerousCore.JournalEvents;

namespace EliteDangerousCore
{
    public class HistoryEntryStatus
    {
        public enum TravelStateType { Docked, Landed, Hyperspace, NormalSpace, Unknown };       // simplifies and stops errors by having an enum

        public string BodyName { get; private set; }
        public int? BodyID { get; private set; }
        public bool HasBodyID { get { return BodyID.HasValue && BodyID.Value >= 0; } }
        public string BodyType { get; private set; }
        public string StationName { get; private set; }
        public string StationType { get; private set; }
        public long? MarketId { get; private set; }
        public TravelStateType TravelState { get; private set; } = TravelStateType.Unknown;  // travel state
        public int ShipID { get; private set; } = -1;
        public string ShipType { get; private set; } = "Unknown";         // and the ship
        public string ShipTypeFD { get; private set; } = "unknown";
        public string OnCrewWithCaptain { get; private set; } = null;     // if not null, your in another multiplayer ship
        public string GameMode { get; private set; } = "Unknown";         // game mode, from LoadGame event
        public string Group { get; private set; } = "";                   // group..
        public bool Wanted { get; private set; } = false;
        public bool BodyApproached { get; private set; } = false;           // set at approach body, cleared at leave body or fsd jump

        private HistoryEntryStatus()
        {
        }

        public HistoryEntryStatus(HistoryEntryStatus prevstatus)
        {
            BodyName = prevstatus.BodyName;
            BodyID = prevstatus.BodyID;
            BodyType = prevstatus.BodyType;
            StationName = prevstatus.StationName;
            StationType = prevstatus.StationType;
            MarketId = prevstatus.MarketId;
            TravelState = prevstatus.TravelState;
            ShipID = prevstatus.ShipID;
            ShipType = prevstatus.ShipType;
            ShipTypeFD = prevstatus.ShipTypeFD;
            OnCrewWithCaptain = prevstatus.OnCrewWithCaptain;
            GameMode = prevstatus.GameMode;
            Group = prevstatus.Group;
            Wanted = prevstatus.Wanted;
            BodyApproached = prevstatus.BodyApproached;
        }

        public static HistoryEntryStatus Update(HistoryEntryStatus prev, JournalEntry je, string curStarSystem)
        {
            if (prev == null)
            {
                prev = new HistoryEntryStatus();
            }

            if (je is JournalLocation)
            {
                JournalLocation jloc = je as JournalLocation;
                TravelStateType t = jloc.Docked ? TravelStateType.Docked : (jloc.Latitude.HasValue ? TravelStateType.Landed : TravelStateType.NormalSpace);

                return new HistoryEntryStatus(prev)     // Bodyapproach copy over we should be in the same state as last..
                {
                    TravelState = t,
                    MarketId = jloc.MarketID,
                    BodyID = jloc.BodyID,
                    BodyType = jloc.BodyType,
                    BodyName = jloc.Body,
                    Wanted = jloc.Wanted,
                    StationName = jloc.StationName.Alt(null),       // if empty string, set to null
                    StationType = jloc.StationType.Alt(null),
                };
            }
            else if (je is JournalCarrierJump)
            {
                var jcj = (je as JournalCarrierJump);
                return new HistoryEntryStatus(prev)     // we are docked on a carrier
                {
                    TravelState = TravelStateType.Docked,
                    MarketId = jcj.MarketID,
                    BodyID = jcj.BodyID,
                    BodyType = jcj.BodyType,
                    BodyName = jcj.Body,
                    Wanted = jcj.Wanted,
                    StationName = jcj.StationName.Alt(null),       // if empty string, set to null
                    StationType = jcj.StationType.Alt(null),
                };
            }
            else if (je is JournalFSDJump)
            {
                var jfsd = (je as JournalFSDJump);
                return new HistoryEntryStatus(prev)
                {
                    TravelState = TravelStateType.Hyperspace,
                    MarketId = null,
                    BodyID = -1,
                    BodyType = "Star",
                    BodyName = jfsd.StarSystem,
                    Wanted = jfsd.Wanted,
                    StationName = null,
                    StationType = null,
                    BodyApproached = false,
                };
            }
            else if (je is JournalLoadGame)
            {
                JournalLoadGame jlg = je as JournalLoadGame;
                bool isbuggy = ShipModuleData.IsSRV(jlg.ShipFD);
                string shiptype = isbuggy ? prev.ShipType : jlg.Ship;
                string shiptypefd = isbuggy ? prev.ShipTypeFD : jlg.ShipFD;
                int shipid = isbuggy ? prev.ShipID : jlg.ShipId;

                return new HistoryEntryStatus(prev) // Bodyapproach copy over we should be in the same state as last..
                {
                    OnCrewWithCaptain = null,    // can't be in a crew at this point
                    GameMode = jlg.GameMode,      // set game mode
                    Group = jlg.Group,            // and group, may be empty
                    TravelState = (jlg.StartLanded || isbuggy) ? TravelStateType.Landed : prev.TravelState,
                    ShipType = shiptype,
                    ShipID = shipid,
                    ShipTypeFD = shiptypefd,
                };
            }
            else if (je is JournalDocked)
            {
                JournalDocked jdocked = (JournalDocked)je;
                return new HistoryEntryStatus(prev)
                {
                    TravelState = TravelStateType.Docked,
                    MarketId = jdocked.MarketID,
                    Wanted = jdocked.Wanted,
                    StationName = jdocked.StationName,
                    StationType = jdocked.StationType,
                };
            }
            else if (je is JournalUndocked)
            {
                return new HistoryEntryStatus(prev)
                {
                    TravelState = TravelStateType.NormalSpace,
                    MarketId = null,
                    StationName = null,
                    StationType = null,
                };
            }
            else if (je is JournalTouchdown)
            {
                if (((JournalTouchdown)je).PlayerControlled == true)        // can get this when not player controlled
                {
                    return new HistoryEntryStatus(prev)
                    {
                        TravelState = TravelStateType.Landed,
                    };
                }
                else
                    return prev;
            }
            else if (je is JournalLiftoff)
            {
                if (((JournalLiftoff)je).PlayerControlled == true)         // can get this when not player controlled
                {
                    return new HistoryEntryStatus(prev)
                    {
                        TravelState = TravelStateType.NormalSpace,
                    };
                }
                else
                    return prev;
            }
            else if (je is JournalSupercruiseExit)
            {
                JournalSupercruiseExit jsexit = (JournalSupercruiseExit)je;
                return new HistoryEntryStatus(prev)
                {
                    TravelState = TravelStateType.NormalSpace,
                    BodyName = (prev.BodyApproached) ? prev.BodyName : jsexit.Body,
                    BodyType = (prev.BodyApproached) ? prev.BodyType : jsexit.BodyType,
                    BodyID = (prev.BodyApproached) ? prev.BodyID : jsexit.BodyID,
                };
            }
            else if (je is JournalSupercruiseEntry)
            {
                return new HistoryEntryStatus(prev)
                {
                    TravelState = TravelStateType.Hyperspace,
                    BodyName = !prev.BodyApproached ? curStarSystem : prev.BodyName,
                    BodyType = !prev.BodyApproached ? "Star" : prev.BodyType,
                    BodyID = !prev.BodyApproached ? -1 : prev.BodyID,
                };
            }
            else if (je is JournalApproachBody)
            {
                JournalApproachBody jappbody = (JournalApproachBody)je;
                return new HistoryEntryStatus(prev)
                {
                    BodyApproached = true,
                    BodyType = jappbody.BodyType,
                    BodyName = jappbody.Body,
                    BodyID = jappbody.BodyID,
                };
            }
            else if (je is JournalApproachSettlement)
            {
                JournalApproachSettlement jappsettlement = (JournalApproachSettlement)je;
                return new HistoryEntryStatus(prev)
                {
                    BodyApproached = true,
                    BodyType = jappsettlement.BodyType,
                    BodyName = jappsettlement.BodyName,
                    BodyID = jappsettlement.BodyID,
                };
            }
            else if (je is JournalLeaveBody)
            {
                JournalLeaveBody jlbody = (JournalLeaveBody)je;
                return new HistoryEntryStatus(prev)
                {
                    BodyApproached = false,
                    BodyType = "Star",
                    BodyName = curStarSystem,
                    BodyID = -1,
                };
            }
            else if (je is JournalStartJump)
            {
                if (prev.TravelState != TravelStateType.Hyperspace) // checking we are into hyperspace, we could already be if in a series of jumps
                {
                    return new HistoryEntryStatus(prev)
                    {
                        TravelState = TravelStateType.Hyperspace,
                    };
                }
                else
                    return prev;
            }
            else if (je is JournalShipyardBuy)
            {
                return new HistoryEntryStatus(prev)
                {
                    ShipID = -1,
                    ShipType = ((JournalShipyardBuy)je).ShipType  // BUY does not have ship id, but the new entry will that is written later - journals 8.34
                };
            }
            else if (je is JournalShipyardNew)
            {
                JournalShipyardNew jsnew = (JournalShipyardNew)je;
                return new HistoryEntryStatus(prev)
                {
                    ShipID = jsnew.ShipId,
                    ShipType = jsnew.ShipType,
                    ShipTypeFD = jsnew.ShipFD,
                };
            }
            else if (je is JournalShipyardSwap)
            {
                JournalShipyardSwap jsswap = (JournalShipyardSwap)je;
                return new HistoryEntryStatus(prev)
                {
                    ShipID = jsswap.ShipId,
                    ShipType = jsswap.ShipType,
                    ShipTypeFD = jsswap.ShipFD,
                };
            }
            else if (je is JournalJoinACrew)
            {
                return new HistoryEntryStatus(prev)
                {
                    OnCrewWithCaptain = ((JournalJoinACrew)je).Captain
                };
            }
            else if (je is JournalQuitACrew)
            {
                return new HistoryEntryStatus(prev)
                {
                    OnCrewWithCaptain = null
                };
            }
            else if (je is JournalDied)
            {
                return new HistoryEntryStatus(prev)
                {
                    BodyName = "Unknown",
                    BodyID = -1,
                    BodyType = "Unknown",
                    StationName = "Unknown",
                    StationType = "Unknown",
                    MarketId = null,
                    TravelState = TravelStateType.Docked,
                    OnCrewWithCaptain = null,
                    BodyApproached = false,     // we have to clear this, we can't tell if we are going back to another place..
                };
            }
            else if (je is JournalLoadout)
            {
                var jloadout = (JournalLoadout)je;
                if (!ShipModuleData.IsSRV(jloadout.ShipFD))     // just double checking!
                {
                    return new HistoryEntryStatus(prev)
                    {
                        ShipID = jloadout.ShipId,
                        ShipType = jloadout.Ship,
                        ShipTypeFD = jloadout.ShipFD,
                    };
                }
                else
                    return prev;
            }
            else
            {
                return prev;
            }
        }
    }

}