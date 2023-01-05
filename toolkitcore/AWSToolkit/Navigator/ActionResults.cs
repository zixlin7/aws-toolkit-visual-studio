﻿using System;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Navigator
{
    public class ActionResults
    {
        Dictionary<string, object> _parameters = new Dictionary<string,object>();

        public bool Cancelled { get; set; } = false;

        public bool Success
        {
            get;
            set;
        }

        public Exception Exception { get; set; }

        public static ActionResults CreateCancelled()
        {
            return new ActionResults()
                .WithCancelled(true)
                .WithSuccess(false);
        }

        public static ActionResults CreateFailed(Exception exception = null)
        {
            return new ActionResults()
                .WithSuccess(false)
                .WithException(exception);
        }

        public ActionResults WithCancelled (bool cancelled)
        {
            Cancelled = cancelled;
            return this;
        }

        public ActionResults WithException(Exception exception)
        {
            Exception = exception;
            return this;
        }

        public ActionResults WithSuccess(bool success)
        {
            this.Success = success;
            return this;
        }

        public string FocalName
        {
            get;
            set;
        }

        public ActionResults WithFocalname(string focalName)
        {
            this.FocalName = focalName;
            return this;
        }

        public bool ShouldRefresh
        {
            get;
            set;
        }

        public ActionResults WithShouldRefresh(bool shouldRefresh)
        {
            this.ShouldRefresh = shouldRefresh;
            return this;
        }

        public bool RunDefaultAction
        {
            get;
            set;
        }

        public ActionResults WithRunDefaultAction(bool runDefaultAction)
        {
            this.RunDefaultAction = runDefaultAction;
            return this;
        }

        public Dictionary<string, object> Parameters => this._parameters;

        public T GetParameter<T>(string key, T defaultValue)
        {
            if (!this.Parameters.ContainsKey(key) || !(this.Parameters[key] is T))
                return defaultValue;
           
            return (T)this.Parameters[key];
        }

        public ActionResults WithParameter(string key, object value)
        {
            return this.WithParameters(new KeyValuePair<string, object>(key, value));
        }

        public ActionResults WithParameters(params KeyValuePair<string, object>[] parameters)
        {
            foreach (KeyValuePair<string, object> kvp in parameters)
            {
                this.Parameters[kvp.Key] = kvp.Value;
            }

            return this;
        }
    }
}
