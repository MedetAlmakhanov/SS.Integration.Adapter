//Copyright 2014 Spin Services Limited

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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SS.Integration.Adapter.MarketRules;
using SS.Integration.Adapter.MarketRules.Model;
using SS.Integration.Adapter.Model;
using SS.Integration.Adapter.Model.Enums;
using SS.Integration.Adapter.Model.Interfaces;

namespace SS.Integration.Adapter.Tests
{
    [TestFixture]
    public class MarketsFilterTests
    {
        private Fixture _snapshot;

        private Mock<Market> _market1;

        private Mock<Market> _market2;

        private Mock<Market> _market3;

        private IObjectProvider<IMarketStateCollection> _objectProvider;
        private IMarketStateCollection _marketStorage = new MarketStateCollection();
            
            
        [SetUp]
        public void SetUp()
        {
            SetUpSnapshotAndMarkets();

            var objectProviderMock = new Mock<IObjectProvider<IMarketStateCollection>>();

            var returnDictionary = new Func<IMarketStateCollection>(() => _marketStorage);
            objectProviderMock.Setup(x => x.GetObject(It.IsAny<string>())).Returns(returnDictionary);
            
            // if there's a better way of assigning parameter let me know
            objectProviderMock.Setup(x => x.SetObject(It.IsAny<string>(), It.IsAny<IMarketStateCollection>()))
                              .Callback<string, IMarketStateCollection>((s, newState) => _marketStorage = newState);

            _objectProvider = objectProviderMock.Object;
        }

        [Test]
        public void TestMockSetup()
        {
            _marketStorage["123"] = new MarketState ("123");
            _objectProvider.SetObject("Test999",_marketStorage);
            var test = _objectProvider.GetObject("Test999");
            test.HasMarket("123").Should().BeTrue();
        }

        private void SetUpSnapshotAndMarkets(MatchStatus status = MatchStatus.InRunning)
        {
            _snapshot = new Fixture {Id = "123"};
            _snapshot.MatchStatus = ((int)status).ToString();
            _market1 = new Mock<Market>();
            _market2 = new Mock<Market>();
            _market3 = new Mock<Market>();

            // Initial Active Market
            //_market1.Setup(m => m.IsActive).Returns(true);

            _market1.SetupAllProperties();
            _market1.Setup(m => m.Selections).Returns(GetSelections(true, false));
            _market1.Setup(m => m.Id).Returns("One");
            _market1.Setup(m => m.Name).Returns("One");

            // Initial Inactive (pending) Market
            //_market2.Setup(m => m.IsActive).Returns(false);

            _market2.SetupAllProperties();
            _market2.Setup(m => m.Selections).Returns(GetSelections(false, false));
            _market2.Setup(m => m.IsPending).Returns(true);
            _market2.Setup(m => m.Id).Returns("Two");
            _market2.Setup(m => m.Name).Returns("Two");


            // Initial Active Market
            //_market3.Setup(m => m.IsActive).Returns(true);
            _market3.SetupAllProperties();
            _market3.Setup(m => m.Selections).Returns(GetSelections(true, false));
            _market3.Setup(m => m.Id).Returns("Three");
            _market3.Setup(m => m.Name).Returns("Three");

            _snapshot.Markets.Add(_market1.Object);
            _snapshot.Markets.Add(_market2.Object);
            _snapshot.Markets.Add(_market3.Object);
        }

        [Test]
        public void ShouldRemoveInactiveMarketsFromSnapshot()
        {
            // 1) Filter is created with initial snapshot
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            var filteredMarkets = new MarketsRulesManager(_snapshot,_objectProvider, rules);

            _snapshot.MatchStatus = "40";

            // 2) Markets are already created and first update arrives
            filteredMarkets.ApplyRules(_snapshot);

            _snapshot.Markets.Should().Contain(_market1.Object);
            _snapshot.Markets.Should().NotContain(_market2.Object);
            _snapshot.Markets.Should().Contain(_market3.Object);
        }

        [Test]
        public void ShouldRemoveInactiveMarketsFromMultipleUpdates()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var filteredMarkets = new MarketsRulesManager(_snapshot,_objectProvider, rules);

            _snapshot.MatchStatus = "40";

            // 2) Markets are already created and first update arrives
            filteredMarkets.ApplyRules(_snapshot);
            filteredMarkets.CommitChanges();

            _snapshot.Markets.Should().Contain(_market1.Object);
            _snapshot.Markets.Should().NotContain(_market2.Object);  // not sent as market2 is still inactive
            _snapshot.Markets.Should().Contain(_market3.Object);

            // 3) New update arrives but market1 is inactive this time
            SetUpSnapshotAndMarkets();
            _market1.Setup(s => s.Selections).Returns(GetSelections(false, false));
            _market1.Setup(m => m.IsPending).Returns(true);

            filteredMarkets.ApplyRules(_snapshot);
            filteredMarkets.CommitChanges();

            _snapshot.Markets.Should().Contain(m=> AreIdsEqual(m,_market1));     // market1 will update with its new status of pending
            _snapshot.Markets.Should().NotContain(m=> AreIdsEqual(m,_market2));  // market2 is still inactive
            _snapshot.Markets.Should().Contain(m=> AreIdsEqual(m,_market3));     // no changes for active market3

            // 4) New update arrives with no changes (market1 is still inactive)
            SetUpSnapshotAndMarkets(); 
            _market1.Setup(s => s.Selections).Returns(GetSelections(false, false));
            _market1.Setup(m => m.IsPending).Returns(true);

            filteredMarkets.ApplyRules(_snapshot);
            filteredMarkets.CommitChanges();

            _snapshot.Markets.Should().NotContain(m=> AreIdsEqual(m,_market1));  // market1 will not update as it was inactive before and still inactive
            _snapshot.Markets.Should().NotContain(m=> AreIdsEqual(m,_market2));  // no changes for inactive market2
            _snapshot.Markets.Should().Contain(m=> AreIdsEqual(m,_market3));     // no changes for active market3
        }

        private bool AreIdsEqual(Market market, Mock<Market> mockMarket)
        {
            return market.Id == mockMarket.Object.Id;
        }

        [Test]
        public void ShouldNotRemoveInactiveMarketsWhenNameChanges()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var filteredMarkets = new MarketsRulesManager(_snapshot,_objectProvider, rules);

            // 2) Markets are already created and first update arrives with a change in name for inactive market2
            SetUpSnapshotAndMarkets();
            _snapshot.MatchStatus = "40";
            _market2.Setup(m => m.Name).Returns("Market Two with new name");

            filteredMarkets.ApplyRules(_snapshot);

            _snapshot.Markets.Should().Contain(_market1.Object);   // market1 updates as is active
            _snapshot.Markets.Should().Contain(_market2.Object);   // market2 updates as its name changed even though is still inactive
            _snapshot.Markets.Should().Contain(_market3.Object);   // market3 updates as is active
        }

        [Test]
        public void ShouldNotRemoveInactiveMarketsWhenStatusChanges()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var filteredMarkets = new MarketsRulesManager(_snapshot,_objectProvider, rules);

            // 2) Markets are already created and first update arrives with a change in status for inactive market2
            SetUpSnapshotAndMarkets();

            _snapshot.MatchStatus = "40";

            _market2.Setup(m => m.IsPending).Returns(false);
            _market2.Setup(m => m.IsSuspended).Returns(true);     // now this market is suspended (still inactive)

            filteredMarkets.ApplyRules(_snapshot);


            _snapshot.Markets.Should().Contain(_market1.Object);   // market1 updates as is active
            _snapshot.Markets.Should().Contain(_market2.Object);   // market2 updates as its name changed even though is still inactive
            _snapshot.Markets.Should().Contain(_market3.Object);   // market3 updates as is active
        }

        [Test]
        public void ShouldNotRemoveInactiveMarketsWhenGetsActive()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var filteredMarkets = new MarketsRulesManager(_snapshot,_objectProvider, rules);

            // 2) Markets are already created and first update arrives with a change in status for inactive market2
            SetUpSnapshotAndMarkets();
            _snapshot.MatchStatus = "40";

            _market2.Setup(m => m.IsPending).Returns(false);
            _market2.Setup(m => m.IsActive).Returns(true);     

            filteredMarkets.ApplyRules(_snapshot);
            
            _snapshot.Markets.Should().Contain(_market1.Object);   // market1 updates as is active
            _snapshot.Markets.Should().Contain(_market2.Object);   // market2 updates as is now active
            _snapshot.Markets.Should().Contain(_market3.Object);   // market3 updates as is active
        }

        [Test]
        public void MarketNameChangedShouldNotRemove()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var filteredMarkets = new MarketsRulesManager(_snapshot,_objectProvider, rules);
            _snapshot.MatchStatus = "40";
            _market2.Setup(x => x.Name).Returns("NewName");
            filteredMarkets.ApplyRules(_snapshot);

            _snapshot.Markets.Should().Contain(_market2.Object);
        }

        [Test]
        public void AutoVoidForUnsettledMarkets()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var marketsFilter = new MarketsRulesManager(_snapshot, _objectProvider, rules);

            _snapshot.MatchStatus = "50";
            _snapshot.Markets.RemoveAll(m => m.Id == _market2.Object.Id);

            _market1.Setup(x => x.Selections).Returns(GetSettledSelections());
            _market3.Setup(x => x.Selections).Returns(GetSelections(SelectionStatus.Void, false));

            marketsFilter.ApplyRules(_snapshot);

            marketsFilter.CommitChanges();

            _snapshot.Markets.Exists(m => m.Id == _market2.Object.Id).Should().BeTrue();
            var marketVoided = _snapshot.Markets.First(m => m.Id == _market2.Object.Id);
            marketVoided.Should().NotBeNull();
            marketVoided.Selections.All(s => s.Status == "3").Should().BeTrue();
        }

        [Test]
        public void AutoVoidForUnsettledMarketsShouldNotAffectUnsettledFixtures()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var marketsFilter = new MarketsRulesManager(_snapshot, _objectProvider, rules);
            _snapshot.MatchStatus = "10";

            marketsFilter.ApplyRules(_snapshot);

            _snapshot.Markets.RemoveAll(m => m.Id == _market2.Object.Id);

            _market1.Setup(x => x.Selections).Returns(GetSettledSelections());
            _market3.Setup(x => x.Selections).Returns(GetSelections("3", false));

            marketsFilter.ApplyRules(_snapshot);

            _snapshot.Markets.Exists(m => m.Id == _market2.Object.Id).Should().BeFalse();


            marketsFilter.ApplyRules(_snapshot);

            _snapshot.Markets.Exists(m => m.Id == _market2.Object.Id).Should().BeFalse();
        }

        [Test]
        public void AutoVoidingIgnoresPreviouslyRemovedSettledMarkets()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var marketsFilter = new MarketsRulesManager(_snapshot, _objectProvider, rules);

            _snapshot.MatchStatus = "50";
            _market1.Setup(x => x.Selections).Returns(GetSettledSelections());
            _market1.Setup(x => x.IsResulted).Returns(true);
            marketsFilter.ApplyRules(_snapshot);
            marketsFilter.CommitChanges();

            _snapshot.Markets.RemoveAll(m => m.Id == _market2.Object.Id);
            
            _market3.Setup(x => x.Selections).Returns(GetSelections("3", false));
            _market3.Setup(x => x.IsResulted).Returns(true);

            marketsFilter.ApplyRules(_snapshot);
            marketsFilter.CommitChanges();

            _snapshot.Markets.Exists(m => m.Id == _market1.Object.Id).Should().BeFalse();

            marketsFilter.ApplyRules(_snapshot);

            _snapshot.Markets.Exists(m => m.Id == _market1.Object.Id).Should().BeFalse();
        }


        [Test]
        public void AutoVoidingEverythingThatWasntVoided()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var marketsFilter = new MarketsRulesManager(_snapshot, _objectProvider, rules);
            _snapshot.MatchStatus = "40";

            _market1.Setup(x => x.Selections).Returns(GetSettledSelections());
            _market1.Setup(x => x.IsResulted).Returns(true);
            
            marketsFilter.ApplyRules(_snapshot);
            marketsFilter.CommitChanges();

            _snapshot.MatchStatus = "50";
            var market3Selections = GetSelections(SelectionStatus.Pending, false);

            _market2.Setup(x=> x.IsResulted)
                .Returns(() => _market2.Object.Selections.All(s => s.Status == SelectionStatus.Void || s.Status == SelectionStatus.Settled));

            _market3.Setup(x => x.Selections).Returns(market3Selections);
            _market3.Setup(x => x.IsResulted)
                    .Returns(() => _market3.Object.Selections.All(s => s.Status == SelectionStatus.Void || s.Status == SelectionStatus.Settled));

            marketsFilter.ApplyRules(_snapshot);
            _snapshot.Markets.Exists(m => m.Id == _market1.Object.Id).Should().BeFalse();
            _snapshot.Markets.Exists(m => m.Id == _market3.Object.Id).Should().BeTrue();
            
            marketsFilter.CommitChanges();

            _snapshot.Markets.First(m => m.Id == _market2.Object.Id).IsResulted.Should().BeTrue();
            _snapshot.Markets.Exists(m => m.Id == _market1.Object.Id).Should().BeFalse();

            _snapshot.Markets.First(m => m.Id == _market3.Object.Id).Should().NotBeNull();
            
            //market 3 was previously active so it shouldn't be voided
            _snapshot.Markets.First(m => m.Id == _market3.Object.Id).IsResulted.Should().BeFalse();
        }

        [Test]
        public void SuspendAllMarketsTest()
        {
            List<IMarketRule> rules = new List<IMarketRule> { VoidUnSettledMarket.Instance, InactiveMarketsFilteringRule.Instance };
            // 1) Filter is created with initial snapshot
            var marketsFilter = new MarketsRulesManager(_snapshot, _objectProvider, rules);
            _market1.Setup(x => x.Selections).Returns(GetSelections(true, false));
            _market2.Setup(x => x.Selections).Returns(GetSelections(true, false));
            _market3.Setup(x => x.Selections).Returns(GetSelections(true, false));

            _snapshot.MatchStatus = "40";
            marketsFilter.ApplyRules(_snapshot);

            _snapshot.Markets.Count.Should().Be(3);
            _snapshot.Markets.All(
                x => x.Selections.TrueForAll(s => s.Tradable.HasValue && s.Tradable.Value)).Should().BeTrue();

            var snapshotWithAllMarketsSuspended = marketsFilter.GenerateAllMarketsSuspenssion();

            snapshotWithAllMarketsSuspended.Markets.Count.Should().Be(3);
            snapshotWithAllMarketsSuspended.Markets.All(m => m.IsSuspended);
            snapshotWithAllMarketsSuspended.Markets.All(
                x => x.Selections.TrueForAll(s => s.Tradable.HasValue && !s.Tradable.Value)).Should().BeTrue();
        }


        public void ShouldRemovePendingMarkets()
        {
            // here I want to test that all the markets
            // in pending state (that never haven't been active before)
            // are removed from the snapshot through the PendingMarketFilteringRule

            List<IMarketRule> rules = new List<IMarketRule> { 
                VoidUnSettledMarket.Instance, 
                InactiveMarketsFilteringRule.Instance,
                new PendingMarketFilteringRule()
            };

            
            var filteredMarkets = new MarketsRulesManager(_snapshot, _objectProvider, rules);

            // 2) Markets are already created and first update arrives with a change in status for inactive market2
            SetUpSnapshotAndMarkets();

            _snapshot.MatchStatus = "40";
            // market1 and market2 are in pending state, while market3 is active
            _market1.Setup(x => x.Selections).Returns(GetSelections(SelectionStatus.Pending, false));
            _market2.Setup(x => x.Selections).Returns(GetSelections(SelectionStatus.Pending, false));
            _market3.Setup(x => x.Selections).Returns(GetSelections(SelectionStatus.InPlay, true));
            
            filteredMarkets.ApplyRules(_snapshot);


            _snapshot.Markets.Should().NotContain(_market1.Object);   // market1 should have been removed
            _snapshot.Markets.Should().NotContain(_market2.Object);   // market2 should have been removed
            _snapshot.Markets.Should().Contain(_market3.Object);      // market3 should be there
        }

        /*[Test]
        public void TortureCacheTest()
        {
            var binaryStore = new BinaryStoreProvider<IDictionary<string, MarketState>>(Environment.CurrentDirectory, "CacheTest-{0}");

            var cacheProvider = new CachedObjectStoreWithPersistance<IDictionary<string, MarketState>>(binaryStore, "Jed", 5);
            bool active = true;

            var snapshot = Helper.GetInPlaySnapshot();
            snapshot.Markets.ForEach(m => m.Selections.ForEach(s => s.Status = !active ? "1" : "0"));
            var marketFilter = new MarketsFilter(snapshot, cacheProvider);
            marketFilter.FilterInactiveMarkets(snapshot);
            marketFilter.CommitChanges();

            for (int i = 0; i < 10; i++)
            {
                snapshot = Helper.GetInPlaySnapshot();
                snapshot.Markets.ForEach(m => m.Selections.ForEach(s => s.Status = active ? "1" : "0"));
                marketFilter.FilterInactiveMarkets(snapshot);
                var sleep = new Random().Next(1, 10);
                Debug.WriteLine(string.Format("Sleeping for {0}", sleep));
                Thread.Sleep(TimeSpan.FromSeconds(sleep));

                // should not be changed at this point
                var marketStates = cacheProvider.GetObject(snapshot.Id);
                marketStates.Values.All(m => m.IsActive == !active).Should().BeTrue();
                
                Debug.WriteLine("Uncommited changes test passed");

                marketFilter.CommitChanges();

                marketStates = cacheProvider.GetObject(snapshot.Id);
                marketStates.Values.All(m => m.IsActive == active).Should().BeTrue();

                Debug.WriteLine("Commited changes test passed");

                active = !active;
            }

            var filePath = Path.Combine(Environment.CurrentDirectory, "CacheTest-" + snapshot.Id);

            Debug.WriteLine(string.Format("File {0}",filePath));
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "CacheTest-" + snapshot.Id)))
            {
                File.Delete(filePath);
                Debug.WriteLine("File deleted");
            }
        }*/

        private List<Selection> GetSettledSelections()
        {
            var selections = GetSelections(SelectionStatus.Settled, true);
            selections[1].Price = 1;

            return selections;
        }

        private List<Selection> GetSelections(string status,bool isSuspended)
        {
            var selections = new List<Selection>
                {
                    new Selection {Id = "1", Tradable = !isSuspended, Status = status },
                    new Selection {Id = "2", Tradable = !isSuspended, Status = status },
                    new Selection {Id = "3", Tradable = !isSuspended, Status = status }
                };

            return selections;
        }

        private List<Selection> GetSelections(bool isActive, bool isSuspended)
        {
            return GetSelections(isActive ? SelectionStatus.InPlay : SelectionStatus.Pending, isSuspended);
        }
    }
}
