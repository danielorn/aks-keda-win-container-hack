using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Billing.BatchSupport.BatchJob.Event
{
    public sealed class ComExecuteWrapper : IDisposable
    {
        private readonly List<object> mArguments = new List<object>();
        private object mComComponent;
        private Type mComType;
        private string mProgId = "";

        public void AddParameter(object parameter) => this.mArguments.Add(parameter);

        public string ExecuteMethod(string progId, string method)
        {
            try
            {
                // Check if we are using the same ProgID, otherwise create a new COM object
                if (this.mProgId != progId)
                {
                    this.mComType = Type.GetTypeFromProgID(progId);
                    if (this.mComType == null)
                        throw new Exception($"Could not find COM type for ProgID [{progId}].");

                    this.mComComponent = Activator.CreateInstance(this.mComType);
                    if (this.mComComponent == null)
                        throw new Exception($"Could not create instance of COM component for ProgID '{progId}'.");

                    this.mProgId = progId;
                }

                // Convert arguments to array
                var arguments = this.mArguments.ToArray();

                // Invoke the method and return the result
                var result = this.mComType.InvokeMember(method, BindingFlags.InvokeMethod, null, this.mComComponent, arguments);
                mArguments.Clear();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing COM method: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            // Release the COM object
            if (this.mComComponent != null)
            {
                Marshal.ReleaseComObject(this.mComComponent);
                this.mComComponent = null;
            }
            GC.SuppressFinalize(this);
        }

        ~ComExecuteWrapper()
        {
            Dispose();
        }
    }
}
