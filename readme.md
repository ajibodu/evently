



currently, you add only consumer on the main evently
and then proceed to addconsumer or configure consumer in individual bus
=> the current issue with this is that
kafka : adds a dlq producer for each added consumer and that is not available for those consumer added directly in main evently
idea: at registration in evently DI, exec a function that finds all implementation of IBaseConsumerConfiguration
        and it call a common function that now adds the Evently consumer to all available bus
        doing the above means we may need to remove the PrepareConfigurations that leaves in individual bus Configuration
