﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LINQPad.Extensibility.DataContext;

namespace RavenLinqpadDriver
{
    public class RavenDriver : StaticDataContextDriver
    {
        RavenConnectionInfo _connInfo;

        public override string Author
        {
            get { return "Ronnie Overby"; }
        }

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
        {
            _connInfo = RavenConnectionInfo.Load(cxInfo);
            return string.Format("RavenDB: {0}", _connInfo.Name);
        }

        public override string Name
        {
            get
            {
#if NET35
                return "RavenDB Driver (.NET 3.5)"; 
#else
                return "RavenDB Driver"; 
#endif
            }
        }

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
        {
            _connInfo = isNewConnection
                ? new RavenConnectionInfo { CxInfo = cxInfo }
                : RavenConnectionInfo.Load(cxInfo);



            var win = new RavenConectionDialog(_connInfo);
            var result = win.ShowDialog() == true;

            if (result)
            {
                _connInfo.Save();
                cxInfo.CustomTypeInfo.CustomAssemblyPath = Assembly.GetAssembly(typeof(RavenContext)).Location;
                cxInfo.CustomTypeInfo.CustomTypeName = "RavenLinqpadDriver.RavenContext";
            }

            return result;
        }

        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            _connInfo = RavenConnectionInfo.Load(cxInfo);

            return new[] { new ParameterDescriptor("connInfo", "RavenLinqpadDriver.RavenConnectionInfo") };
        }

        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            _connInfo = RavenConnectionInfo.Load(cxInfo);
            return new[] { _connInfo };
        }

        public override IEnumerable<string> GetAssembliesToAdd()
        {
            var assemblies = new[] { 
                "NLog.dll",
#if NET35
                "Newtonsoft.Json.Net35.dll",
                "Raven.Abstractions-3.5.dll"
#else
                "Newtonsoft.Json.dll",
                "Raven.Abstractions.dll"
#endif
            }.ToList();

            if (_connInfo!= null)
            {
                assemblies.AddRange(_connInfo.GetAssemblyPaths());                
            }

            return assemblies;
        }

        public override IEnumerable<string> GetNamespacesToRemove()
        {
            // linqpad uses System.Data.Linq by default, which isn't needed
            return new[] { "System.Data.Linq" };
        }

        public override IEnumerable<string> GetNamespacesToAdd()
        {
            var namespaces = new List<String>(base.GetNamespacesToAdd());

            namespaces.AddRange(new[] 
            {
                "Raven.Client",
                "Raven.Client.Document",
                "Raven.Abstractions.Data",              
                "Raven.Client.Linq"
            });

            if (_connInfo != null)
                namespaces.AddRange(_connInfo.GetNamespaces());

            return namespaces;
        }

        public override List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type customType)
        {
            return new List<ExplorerItem>();
        }

        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            _connInfo = RavenConnectionInfo.Load(cxInfo);

            var rc = context as RavenContext;
            rc.LogWriter = executionManager.SqlTranslationWriter;
        }

        public override void TearDownContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager, object[] constructorArguments)
        {
            base.TearDownContext(cxInfo, context, executionManager, constructorArguments);
            var rc = context as RavenContext;
            if (rc != null)
                rc.Dispose();
        }
    }
}
