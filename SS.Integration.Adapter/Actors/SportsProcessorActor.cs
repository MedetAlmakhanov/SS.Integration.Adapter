﻿//Copyright 2017 Spin Services Limited

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using Akka.Actor;
using SS.Integration.Adapter.Actors.Messages;
using SS.Integration.Adapter.Interface;

namespace SS.Integration.Adapter.Actors
{
    /// <summary>
    /// This class has the responibility to repeatedly schedule all sports processing at specified interval (default 60 seconds)
    /// Also Statistics Generation is triggered with each interval
    /// </summary>
    public class SportsProcessorActor : ReceiveActor
    {
        #region Constants

        public const string ActorName = nameof(SportsProcessorActor);
        public const string Path = "/user/" + ActorName;
        private readonly ICancelable _processSportsMsgSchedule;
        #endregion

        #region Attribues

        private readonly IServiceFacade _serviceFacade;
        private readonly IActorRef _sportProcessorRouterActor;
        private readonly IActorRef _statsActor;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="serviceFacade"></param>
        /// <param name="sportProcessorRouterActor"></param>
        public SportsProcessorActor(
            ISettings settings,
            IServiceFacade serviceFacade,
            IActorRef sportProcessorRouterActor)
        {
            _serviceFacade = serviceFacade ?? throw new ArgumentNullException(nameof(serviceFacade));
            _sportProcessorRouterActor = sportProcessorRouterActor ?? throw new ArgumentNullException(nameof(sportProcessorRouterActor));

            Receive<ProcessSportsMsg>(o => ProcessSports());

            _processSportsMsgSchedule = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(settings.FixtureCheckerFrequency),
                Self,
                new ProcessSportsMsg(),
                Self);
        }

        #endregion

        #region Private methods

        protected override void PreRestart(Exception reason, object message)
        {
            _processSportsMsgSchedule.Cancel();
            base.PreRestart(reason, message);
        }

        private void ProcessSports()
        {
            var sports = _serviceFacade.GetSports();

            foreach (var sport in sports)
            {
                _sportProcessorRouterActor.Tell(new ProcessSportMsg {Sport = sport.Name});
            }
        }

        #endregion
    }
}