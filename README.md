OpenTraits.QuickDeployService
=============================

If you're developing cloud apps in Azure you're probably already familiar with the deployment process that takes 15 minutes.
So for every small modification you need to redeploy the whole app and then wait before being able to test it.
Some may debate that it's not a good practice to deploy minor changes into production.
Personally I like the continuous deployment principle.


This is a .NET library to quickly deploy individual files to Azure cloud apps using Azure Blob Storage. 


## How it works?

This library will check a Azure Blob container for newer files every configurable time interval. 
So files you upload to a "deploy" container will automatically propagate on all machines.
Note that it's recommended that you still do "standard" deployments on a regular interval just so Azure has the proper packages to create new machines.

But it should probably work anyway, because Azure creates a machine from an older image and then this service will copy all the new files from the Azure Blob.


### Method 1: Using Azure Startup Tasks

In your ServiceDefinition.csdef file you can specify startup tasks that Azure will execute.
The code below will configure this library to start in the background with elevated permissions so it can update the files.
By default, the Application Pool identity (Network Service) had read-only access to the application files.

Personally I __recommend__ this method since it's isolated from the app, it requires less configuration and most importantly it doesn't require changing the permissions for Network Service which could raise other security issues.


#### Configuration

##### In ServiceDefinition.csdef:
```xml
<ServiceDefinition>
    ...
    <WebRole>
        ...
        <Startup>
            <Task commandLine="OpenTraits.AzureQuickDeploy.Cli.exe" 
                  executionContext="elevated" 
                  taskType="background" />
        </Startup>
        ...
    </WebRole>
    ...
</ServiceDefinition>
```

##### In App.Config
Under App.config of OpenTraits.AzureQuickDeploy.Cli project you have to configure the Azure Blob connection string.
Then, copy OpenTraits.AzureQuickDeploy.Cli.exe.config to your project and have it copy in the same folder with OpenTraits.AzureQuickDeploy.Cli.exe and OpenTraits.AzureQuickDeploy.Cli.dll.



### Method 2: Starting the service from you application startup code.

This service is started once, for example from Application_Start or from a Singleton application object.

The big problem here is that Network Service has read only access to the application folder.
So you'll want first to run a startup task that gives write permissions.


#### Configuration

##### In Global.asax.cs:
```cs
protected void Application_Start()
{
    // check container "deploy" every 5 seconds for newer files
    OpenTraits.Azure.QuickDeployService.CheckAsync(5, "deploy", rootAppPath);
}
```


##### In ServiceDefinition.csdef:
```xml
<ServiceDefinition>
    ...
    <WebRole>
        ...
        <Startup>
            <Task commandLine="startup.cmd" 
                  executionContext="elevated" 
                  taskType="background" />
        </Startup>
        ...
    </WebRole>
    ...
</ServiceDefinition>
```

##### In startup.cmd:

```bat
icacls "e:\sitesroot\0" /grant "Network Service":(OI)(CI)F
icacls "f:\sitesroot\0" /grant "Network Service":(OI)(CI)F
```

Make sure to deploy startup.cmd in the bin folder as well. 

Just stating it again, method 2 presents a security risk since it gives write permissions to the Network Service, so security holes in code can have more severe consequences.
Unless you have a very strong reason, stick to method 1.

-- 

This service is used in production on [opentraits.com](http://opentraits.com)

