using Azure.Messaging.ServiceBus.Administration;
using Evently.Azure.ServiceBus.Constants;

namespace Evently.Azure.ServiceBus.Builders;

public class RuleOptionBuilder<TEvent>
{
    private readonly List<CreateRuleOptions> _rules = new();

    public RuleOptionBuilder<TEvent> WithRule(string name, RuleFilter ruleFilter)
    {
        _rules.Add(new CreateRuleOptions(name, ruleFilter));
        return this;
    }

    public RuleOptionBuilder<TEvent> WithSqlRule(string name, string key, string value)
    {
        _rules.Add(new CreateRuleOptions(name, new SqlRuleFilter($"{key} = '{value}'")));
        return this;
    }

    public RuleOptionBuilder<TEvent> WithSqlEventTypeRule()
    {
        _rules.Add(new CreateRuleOptions(typeof(TEvent).Name, new SqlRuleFilter($"{Default.EventType} = '{typeof(TEvent).FullName}'")));
        return this;
    }

    public List<CreateRuleOptions> GetRules() => _rules;
}