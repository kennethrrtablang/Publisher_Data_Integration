# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references


2. Visual Studio Professional with Data storage and processing as well as the Microsoft DataTools Integration Services extensions

# Build and Test

No special build requirements.

Run requires setup of local.settings.json files for both DLL Core Tester and PDI_Azure_Function projects respectively

DLL Core Tester
>{
  "PDI_ConnectionString": "",
  "PUB_ConnectionString": "",
  "sapdi": "", 
  "TemplateContainer": "pdi-validation-templates",
  "FileFolder": "C:\\Users\\Scott\\source\\Sample Files\\",
  "ValidationFolder": "C:\\Users\\Scott\\source\\Sample Files\\Validation\\",
  "SMTP_Account": "apikey",
  "SMTP_Password": "",
  "SMTP_FromEmail": "pdi_support@investorcom.com",
  "SMTP_FromName": "PDI Local DEV"
}

PDI_Azure_Function_
>{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "sapdi": "",
    "PDI_ConnectionString": "",
    "PUB_ConnectionString": "",
    "IncomingContainer": "pdi-incoming",
    "IncomingBatchContainer": "pdi-sftp-test", //"pdi-incoming-batch"
    "ProcessingContainer": "pdi-processing",
    "ResultContainer": "pdi-result",
    "RejectedContainer": "pdi-rejected",
    "CompletedContainer": "pdi-completed",
    "TemplateContainer": "pdi-validation-templates",
    "ArchiveContainer": "pdi-archive",
    "SMTP_Account": "apikey",
    "SMTP_Password": "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",
    "SMTP_FromEmail": "pdi_support@investorcom.com",
    "SMTP_FromName": "PDI Local DEV",
    "PublisherQueueName": "pdi-import"
  }
}

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)
