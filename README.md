# OrientDB.Net.Core.BusinessObjects
A wrapper around the official OrientDB API with the possibility to generate business model classes.

## Introduction to OrientDB.Net.Core.BusinessObjects
"OrientDB.Net.Core.BusinessObjects" is a business layer that wrapps the office .NET API for [OrientDB](https://orientdb.com/). 
The BusinessObject allows an easy access to the database. 
The included generator uses JSON files to generate classes to manipulate the documents in the database.

### Getting started
First you need to download the latest release version of "OrientDB.Net.Core.BusinessObjects.Generator" 
[Link](https://github.com/kostkams/OrientDB.Net.Core.BusinessObjects/releases) or simply checkout and build the code.

### Create the data model
The data model for your business domain will be generated based on a set of description JSON files.

Now create a new JSON file and name it "Tutorial.Person.bo.json" (the name doesn't care). 
Add the following content to the file.
```json
{
  "Name": "Tutorial",
  "Namespace": "Tutorial",
  "Projects": [
    {
      "Name": "Person",
      "BusinessObject": {
        "Types":[
          {
            "IsRoot": true,
            "Name": "Person",
            "ClassName": "Person",
            "Properties": [
              {  
                "Name": "LastName",
                "Type": "String",
                "Required": true,
                "DocumentPropertyName": "LastName"
             },
             {
                "Name": "FirstName",
                "Type": "String",
                "Required": true,
                "DocumentPropertyName": "FirstName"
             }
            ]
          }
        ]
      }
    }
  ]
}
```
* `"Name": "Tutorial"` is the name of the whole generated project
* `"Namespace": "Tutorial"` is the namespace for classes and interfaces
* `"Projects"` each item describes a new generated project
* `"Name": "Person"` name of the project
* `"BusinessObject"` begin of the model description
* `"Types"` represents a model class
* `"IsRoot": true` can the class be constructed
* `"Name": "Person"` name of the class and interface
* `"ClassName": "Person"` name of the vertex in the database
* `"Properties"` property list for the class
* `"Name": "LastName"` name of the property
* `"Type": "String"` type of the property (String, Integer, Double, Boolean, Guid, DateTime allowed)
* `"Required": true` indicates that the value must be set while its saving in the database
* `"DocumentPropertyName": "LastName"` name of the document property in the database

### Generating the business object
Now you can use the JSON to generate the business objects. 
Open a new console and change the directory to the `OrientDB.Net.Core.BusinessObjects.Generator.exe`.
Use the following command to generate:
```
OrientDB.Net.Core.BusinessObjects.Generator.exe -o \outpuDir -c Tutorial.Person.bo.json
```

### Needed Assembly References
To run the a solution with the generated business objects, the following references are needed:
* OrientDB-Net.binary.Innov8tive.dll (part of the 'OrientDB.Net.Core.BusinessObjects' release package)

The 'OrientDB.Net.Core.BusinessObjects' will be referenced automatically by nuget.

### Accessing the data
In a new _Console_ project (>= .NET Core 3.0) add the following usings:
```C#
using OrientDB.Net.Core.BusinessObjects;
using Persion.Tutorial;
```
In the _Main_ method add the following code:
```C#
private stativ void Main()
{
  var businessDocument = BusinessDocumentFactory.Connect(new ConnectionInfo("localhost",
                                                                            2424,
                                                                            "root",
                                                                            "pwd",
                                                                            "TestDatabase",
                                                                            EDatabaseType.Graph));
  
  // Create a new person
  using (var session = businessDocument.OpenSession()) // Each time OpenSession is called, a new connection to the database is opened
  {
    var transaction = session.BeginTransaction();
    var person = transaction.CreatePerson();
    person.FirstName = "John";
    person.LastName = "Doe";
    transaction.Commit(); // The person will be saved in the database
  }
  
  // Get the person by name
  using (var session = businessDocument.OpenSession())
  {
    session.Get<IPersion>(p => p.FirstName == "John" && p.LastName == "Doe").First();
  }
}
```

### Next...
Description how to use Children and Refereces to other classes are coming soon...
Use `"Children"` and `"ReferenceLists"` in the JSON file
