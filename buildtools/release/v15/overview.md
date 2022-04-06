This edition of the toolkit is for Visual Studio 2017 and 2019.
- There is a separate [AWS Toolkit extension for Visual Studio 2022](https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2022).
- Version 1.27.0.0 is the last version of this extension that supports Visual Studio 2017. Newer versions only support Visual Studio 2019.
- If you require the toolkit for Visual Studio 2013 and/or 2015, please use the installer available for download [here](https://sdk-for-net.amazonwebservices.com/latest/AWSToolsAndSDKForNet.msi).

For issues or questions about this extension please open a GitHub issue at https://github.com/aws/aws-toolkit-visual-studio.

The AWS Toolkit provides Visual Studio project templates that you can use as starting points for AWS console and web applications. As your application runs, you can use the AWS Explorer to view the AWS resources used by the application. For example, if your application creates buckets in Amazon S3, you can use AWS Explorer to view those buckets and their contents. If you need to provision AWS resources for your application, you can create them manually using the AWS Explorer or use the CloudFormation templates included with the AWS Toolkit to provision web application environments hosted on Amazon EC2.

* The AWS Explorer presents a tree view of your AWS resources such as Amazon EC2, Amazon S3, Amazon DynamoDB, AWS Lambda, AWS CloudFormation and other services as well. With the AWS Explorer   you can view and edit resources within these services.

* Web Applications and Web Sites can be deployed to the AWS cloud by right clicking on the project in the Solution Explorer and selecting "Publish to AWS Elastic Beanstalk".

* Serverless applications can be deployed to the AWS cloud by right clicking on the project in the Solution Explorer and selecting "Publish to AWS Lambda".

* Using the Amazon EC2 Instance view you can quickly create new Windows instances and Remote Desktop into them simply by right clicking the instance and selecting "Open Remote Desktop".

* You can browse the files stored in your S3 bucket and upload and download files. You can create pre-signed URLs to objects to pass around and change the permissions of files. If the bucket is used with Amazon CloudFront you can perform invalidation requests from within the bucket browser.

* AWS IAM users and groups can be created and users can be assigned to groups. Access keys can be generated for IAM users and access policies can created using the access policy editor for both users and groups.

* Through the AWS Explorer, you can view, create, and delete Amazon DynamoDB tables. You can also add new items to tables, add new attributes to items, and edit attribute values. The AWS Toolkit also enables you to search your tables using Scan operations.

* Using the editor for Amazon SQS queues you can see and edit the properties, send messages to the queue and view a sampling of the messages in the queue.

* Using the editor for Amazon SNS topics you can see properties, publish messages to the queue and create subscriptions to the topic.  You can also drag and drop queues onto the topic editor to create subscriptions.
