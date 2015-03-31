﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Processors;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.ECSLib
{
    public class Game
    {
        /// <summary>
        /// Global Entity Manager.
        /// </summary>
        public EntityManager GlobalManager { get { return m_globalManager; } }
        private EntityManager m_globalManager;

        /// <summary>
        /// Singleton Instance of Game
        /// </summary>
        public static Game Instance { get { return m_instance; } }
        private static Game m_instance;

        public Random RNG;

        /// <summary>
        /// List of StarSystems currently in the game.
        /// </summary>
        public List<StarSystem> StarSystems { get; set; }
        public int StarSystemCurrentIndex { get; set; }

        public DateTime CurrentDateTime { get; set; }

        public Engine_Comms EngineComms { get; private set; }       

        public SubpulseLimitRequest NextSubpulse
        {
            get 
            {
                lock (subpulse_lockObj)
                {
                    return m_nextSubpulse;
                }
            }
            set
            {
                lock (subpulse_lockObj)
                {
                    if (m_nextSubpulse == null)
                    {
                        m_nextSubpulse = value;
                        return;
                    }
                    if (value.MaxSeconds < m_nextSubpulse.MaxSeconds)
                    {
                        // Only take the shortest subpulse.
                        m_nextSubpulse = value;
                    }
                }
            }    
        }
        private SubpulseLimitRequest m_nextSubpulse;
        private object subpulse_lockObj = new object();

        public Interrupt CurrentInterrupt { get; set; }

        public Game()
        {
            m_globalManager = new EntityManager();
            m_instance = this;

            RNG = new Random();

            StarSystems = new List<StarSystem>();

            CurrentDateTime = DateTime.Now;

            NextSubpulse = new SubpulseLimitRequest();
            NextSubpulse.MaxSeconds = 5;

            CurrentInterrupt = new Interrupt();

            EngineComms = new Engine_Comms();

            // Setup time Phases.
            PhaseProcessor.Initialize();
        }

        /// <summary>
        /// Runs the game simulation in a loop. Will check for and process messages from the UI.
        /// </summary>
        public void MainGameLoop()
        {
            bool quit = false;
            bool messageProcessed = false;

            while (!quit)
            {
                // lets first check if there are things waiting in a queue:
                if (EngineComms.LibMessagesWaiting() == false)
                {
                    // there is nothing from the UI, so lets sleep for a while before checking again.
                    Wait();
                    continue; // go back and check again.
                }

                // loop through all the incoming queues looking for a new message:
                List<int> factions = m_globalManager.GetAllEntitiesWithDataBlob<PopulationDB>();
                foreach (int faction in factions)
                {
                    // lets just take a peek first:
                    Message message;
                    if (EngineComms.LibPeekFactionInQueue(faction, out message) && IsMessageValid(message))
                    {
                        // we have a valid message we had better take it out of the queue:
                        message = EngineComms.LibReadFactionInQueue(faction);

                        // process it:
                        ProcessMessage(faction, message, ref quit);
                        messageProcessed = true;
                    }
                }

                // lets check if we processed a valid message this time around:
                if (messageProcessed)
                {
                    // so we processed a valid message, better check for a new one right away:
                    messageProcessed = false;
                    continue;
                }
                else
                {
                    // we didn't process a valid message... 
                    // we should probably wait for a while for the pulse to finish or new stuff to queue up
                    Wait();
                }
            }
        }

        private bool IsMessageValid(Message message)
        {
            return true; // we will do this until we have messages that can be invalid!!
        }

        private void ProcessMessage(int faction, Message message, ref bool quit)
        {
            if (message == null)
            {
                return;
            }

            switch (message._messageType)
            {
                case Message.MessageType.Quit:
                    quit = true;                                        // cause the game to quit!
                    break;
                case Message.MessageType.Echo:
                    EngineComms.LibWriteOutQueue(faction, message);     // echo chamber ;)
                    break;
                default:
                    throw new System.Exception("Message of type: " + message._messageType.ToString() + ", Went unprocessed.");
            }
        }

        private void Wait()
        {
            // we should have a better way of doing this
            // is there a way for the EnginComs class to fire an event to wake the thread when 
            // a new message come is??
            // that would be the ideal way to do it, no wasted time, no wasted CPU usage.
            Thread.Sleep(5);  
        }

        /// <summary>
        /// Time advancement code. Attempts to advance time by the number of seconds
        /// passed to it.
        /// 
        /// Interrupts may prevent the entire requested timeframe from being advanced.
        /// </summary>
        /// <param name="deltaSeconds">Time Advance Requested</param>
        /// <returns>Total Time Advanced</returns>
        public int AdvanceTime(int deltaSeconds)
        {
            int timeAdvanced = 0;

            // Clamp deltaSeconds to a multiple of our MinimumTimestep.
            deltaSeconds -= (deltaSeconds % GameSettings.GameConstants.MinimumTimestep);
            if (deltaSeconds == 0)
            {
                deltaSeconds = GameSettings.GameConstants.MinimumTimestep;
            }

            // Clear any interrupt flag before starting the pulse.
            CurrentInterrupt.StopProcessing = false;

            while (!CurrentInterrupt.StopProcessing && deltaSeconds > 0)
            {
                int subpulseTime = Math.Min(NextSubpulse.MaxSeconds, deltaSeconds);
                // Set next subpulse to max value. If it needs to be shortened, it will
                // be shortened in the pulse execution.
                NextSubpulse.MaxSeconds = int.MaxValue;

                // Update our date.
                CurrentDateTime += TimeSpan.FromSeconds(subpulseTime);

                // Execute subpulse phases. Magic happens here.
                PhaseProcessor.Process(subpulseTime);

                // Update our remaining values.
                deltaSeconds -= subpulseTime;
                timeAdvanced += subpulseTime;
            }

            if (CurrentInterrupt.StopProcessing)
            {
                // Notify the user?
                // Gamelog?
                // <@ todo: review interrupt messages.
            }
            return timeAdvanced;
        }

        /// <summary>
        /// Test function to demonstrate the usage of the EntityManager.
        /// </summary>
        public void EntityManagerTests()
        {
            // Create an entity with individual DataBlobs.
            int planet = GlobalManager.CreateEntity();
            GlobalManager.SetDataBlob(planet, OrbitDB.FromStationary(5));
            SpeciesDB species1 = new SpeciesDB("Human", 1, 0.1, 1.9, 1.0, 0.4, 4, 14, -15, 45);
            Dictionary<SpeciesDB,double> pop = new Dictionary<SpeciesDB, double>();
            pop.Add(species1,10);
            GlobalManager.SetDataBlob(planet, new PopulationDB(pop));

            // Create an entity with a DataBlobList.
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(OrbitDB.FromStationary(2));
            GlobalManager.CreateEntity(dataBlobs);

            // Create one more, just for kicks.
            
            Dictionary<SpeciesDB, double> pop2 = new Dictionary<SpeciesDB, double>();
            pop.Add(species1, 10);
            dataBlobs.Add(new PopulationDB(pop2));
            GlobalManager.CreateEntity(dataBlobs);

            // Get all DataBlobs of a specific type.
            List<PopulationDB> populations = GlobalManager.GetAllDataBlobsOfType<PopulationDB>();
            List<OrbitDB> orbits = GlobalManager.GetAllDataBlobsOfType<OrbitDB>();

            // Get all DataBlobs of a specific entity.
            dataBlobs = GlobalManager.GetAllDataBlobsOfEntity(planet);

            // Remove an entity.
            GlobalManager.RemoveEntity(planet);

            // Add a new entity (using a list of DataBlobs.
            GlobalManager.CreateEntity(dataBlobs);

            // Find all entities with a specific DataBlob.
            List<int> populatedEntities = GlobalManager.GetAllEntitiesWithDataBlob<PopulationDB>();

            // Get the Population DB of a specific entity.
            PopulationDB planetPopDB = GlobalManager.GetDataBlob<PopulationDB>(planet);

            // Change the planet Pop.
            planetPopDB.Population[species1] += 5;

            // Get the current value.
            PopulationDB planetPopDB2 = GlobalManager.GetDataBlob<PopulationDB>(planet);

            if (planetPopDB.Population != planetPopDB2.Population)
            {
                // Note, we wont hit this because the value DID change.
                throw new InvalidOperationException();
            }

            // Forget it, remove the DataBlob.
            GlobalManager.RemoveDataBlob<PopulationDB>(planet);

            if (GlobalManager.GetDataBlob<PopulationDB>(1) == null)
            {
                // Will hit this!
                // Entity 1 doesn't have a population, so GetDataBlob returns null.

                // This crap is so you can reliably breakpoint this (even in release mode) without it being optimized away.
                int i = 0;
                i++;
            }
            else
            {
                // Wont hit this!
                // Since there's no pop!
                throw new InvalidOperationException();
            }

            // Remove the crap we added.
            GlobalManager.RemoveEntity(planet);
            GlobalManager.RemoveEntity(planet + 1);
            GlobalManager.RemoveEntity(planet + 2);
        }
    }
}
