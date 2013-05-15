OpenTraits.QuickDeployService
=============================

.NET library to quickly deploy individual files to Azure cloud apps using Azure Blob Storage.
This service is started once and it will run a thread that checks the blob storage for newer files.
It's recommended that you still do deployments on a regular interval just so Azure has the proper packages to create new machines.
Otherwise, new machines will be created with a old deployement - but still, it will update from the blog and it may work if you have all the updated files on the blog.

**Usage**
```cs
protected void Application_Start()
{
    // check container "deploy" every 5 seconds for newer files
    OpenTraits.Azure.QuickDeployService.Check(5, "deploy");
}
```

After this, simply copy files to the "deploy" container and they will automatically propagate on all machines.