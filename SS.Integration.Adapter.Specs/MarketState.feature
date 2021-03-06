﻿Feature: MarketState
	In order to avoid silly mistakes
	As a developer
	I want to make sure that markets' state are correctly computed by looking at the selections

# See SSLN - Connect Data Formats Guide v2.2 document, "Appendix A - Tradability matrix" to understand the output of these tests

# for reference, selection's status
# public const string Pending         = "0";
# public const string Active          = "1";
# public const string Settled         = "2";
# public const string Void            = "3";

@MarketStateComputation
Scenario: Compute Market Tradability (all selection must be pending in order to put the market in a pending state)
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 0      | 0           | -1    |
	| 2         | 0      | 0           | 0     |
	| 3         | 0      | 0           | 1     |
	| 4         | 0      | 0           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 0      | 1       | 1         | 0        |
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 0      | 1           | 0     |
	| 3         | 0      | 1           | 1     |
	| 4         | 0      | 1           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0         | 0        |
	

Scenario: Compute Market Tradability (tradability IS considered for suspension)
	Given I have this market
	| Property | Value |
	| Id       | "1" |
	And The market has these selections
	| Id	    | Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 1      | 1           | 0     |
	| 3         | 1      | 1           | 1     |
	| 4         | 1      | 1           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0         | 0        |
	Given I have this market
	| Property | Value |
	| Id       | "2" |
	And The market has these selections
	# Pay attention here to tradability = false
	| Id		| Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 1      | 1           | 0     |
	| 3         | 1      | 1           | 1     |
	| 4         | 1      | 0           | 2     |  
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0        | 0         |
	Given I have this market
	| Property | Value |
	| Id       | "3" |
	And The market has these selections
	# Pay attention here to tradability = false
	| Id	    | Status | Tradability | Price |
	| 1         | 1      | 0           | -1    |
	| 2         | 1      | 0           | 0     |
	| 3         | 1      | 0           | 1     |
	| 4         | 1      | 0           | 2     |  
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 1        | 0         |

Scenario: Compute Market Tradability (one active selection is enough to make the market active)
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 1      | 0           | -1    |
	| 2         | 0      | 0           | 0     |
	| 3         | 0      | 0           | 1     |
	| 4         | 0      | 0           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 1         | 0        |
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 0      | 0           | 0     |
	| 3         | 0      | 0           | 1     |
	| 4         | 0      | 0           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0         | 0        |
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 2      | 0           | 0     |
	| 3         | 3      | 0           | 1     |
	| 4         | 0      | 0           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0         | 0        |


Scenario: Compute Market Tradability (resulted look at the selection price)
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 3      | 0           | -1    |
	| 2         | 3      | 0           | 0     |
	| 3         | 3      | 0           | 1     |
	| 4         | 3      | 0           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 0      | 0       | 1         | 1        |
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 3      | 1           | -1    |
	| 2         | 3      | 1           | 0     |
	| 3         | 3      | 1           | 1     |
	| 4         | 3      | 1           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 0      | 0       | 1         | 1        |
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id		| Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 1      | 1           | 0     |
	| 3         | 1      | 1           | 1     |
	| 4         | 2      | 1           | 2     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0         | 0        |
	Given I have this market
	| Property | Value |
	| Id       | "ABC" |
	And The market has these selections
	| Id | Status | Tradability | Price |
	| 1         | 1      | 1           | -1    |
	| 2         | 1      | 1           | 0     |
	| 3         | 1      | 1           | 1     |
	| 4         | 2      | 1           | 1     |
	When I infer the market's status
	Then I should have these values
	| Active | Pending | Suspended | Resulted |
	| 1      | 0       | 0         | 0        |


Scenario: Rolling handicap line is stored correctly 
Given MarketStates collection is set up
And Rolling Handicap market wit id=rollingMarket
And The rolling market has these selections
| Id | Status | Tradability | Price | Line |
| 1  | 1      | 0			| -1    | 1    |
| 2  | 1      | 0			|  0    | 1    |
When Market status is generated for snapshot=true
Then Rolling market should have line=1	

Scenario: Rolling handicap line is calculated correctly on updates
Given MarketStates collection is set up
And Rolling Handicap market wit id=rollingMarket
And The rolling market has these selections
| Id | Status | Tradability | Price | Line |
| 1  | 1      | 0           | -1    | 1    |
| 2  | 1      | 0           | 0     | 1    |
When Market status is generated for snapshot=true
Then Rolling market should have line=1	
When Market has been updated 
| Id | Status | Tradability | Price | Line  | 
| 1  | 1      | 0           | 0     |  -2    | 
| 2  | 1      | 0           | 0     |  -2    | 
And Market status is generated for snapshot=false
Then Rolling market should have line=-2	

Scenario: Rolling handicap line is calculated correctly on home/away market
Given MarketStates collection is set up
And Rolling Handicap market wit id=rollingMarket
And The rolling market has these selections
| Id | Status | Tradability | Price | Line | TagKey | TagValue  |
| 1  | 1      | 0           | -1    | -1   | team   |     1     |
| 2  | 1      | 0           | 0     | 1    | team   |     2     |
When Market status is generated for snapshot=true
Then Rolling market should have line=-1	

Scenario: Rolling handicap line is calculated correctly on home/away market during updates
Given MarketStates collection is set up
And Rolling Handicap market wit id=rollingMarket
And The rolling market has these selections
| Id | Status | Tradability | Price | Line | TagKey | TagValue  |
| 1  | 1      | 0           | -1    | -1   | team   |     1     |
| 2  | 1      | 0           | 0     | 1    | team   |     2     |
When Market status is generated for snapshot=true
Then Rolling market should have line=-1	
When Market has been updated 
| Id | Status | Tradability | Price | Line  | 
| 1  | 1      | 0           | 0     |  2    | 
| 2  | 1      | 0           | 0     | -2    | 
And Market status is generated for snapshot=false
Then Rolling market should have line=2	

Scenario: Rolling handicap line is calculated correctly on when only away selection is passed
Given MarketStates collection is set up
And Rolling Handicap market wit id=rollingMarket
And The rolling market has these selections
| Id | Status | Tradability | Price | Line | TagKey | TagValue  |
| 1  | 1      | 0           | -1    | -1   | team   |     1     |
| 2  | 1      | 0           | 0     | 1    | team   |     2     |
When Market status is generated for snapshot=true
Then Rolling market should have line=-1	
When Market has been updated 
| Id | Status | Tradability | Price | Line | 
| 2  | 1      | 0           | 0     | 1    | 
And Market status is generated for snapshot=false
Then Rolling market should have line=-1	