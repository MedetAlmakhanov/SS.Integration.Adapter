﻿//Copyright 2014 Spin Services Limited

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

namespace SS.Integration.Adapter.Diagnostics.RestService.Models
{
    public class FixtureProcessingEntry
    {
        public enum FixtureProcessingState 
        {
            PROCESSED = 0,
            PROCESSING = 1,
            SKIPPED = 2,
        }

        public DateTime Timestamp { get; set; }

        public string Sequence { get; set; }

        public string Epoch { get; set; }

        public string EpochChangeReason { get; set; }

        public bool IsUpdate { get; set; }

        public string Exception { get; set; }

        public FixtureProcessingState State { get; set; }
    }
}