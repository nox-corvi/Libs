using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Reflection;
using System.Runtime.Serialization;
using System.IO;

namespace Nox
{
    /// <summary>
    /// Stellt Daten für ein abbrechbares Ereignis bereit. 
    /// </summary>
    public class CancelWithMessageEventArgs : System.ComponentModel.CancelEventArgs
    {
        #region Property 
        public string Message { get; set; }
        #endregion

        public CancelWithMessageEventArgs() : base() { }

        public CancelWithMessageEventArgs(bool Cancel) : base(Cancel) { }

        public CancelWithMessageEventArgs(bool Cancel, string Message) : base(Cancel) { this.Message = Message; }
    }

    /// <summary>
    /// Stellt Daten bereit wenn der XMLParser einen Block betritt.
    /// </summary>
    public class EnterBlockEventArgs : CancelWithMessageEventArgs
    {

        #region Property 
        public string BlockName { get; set; }

        public string History { get; set; }

        public string[] SubBlocks { get; set; }
        public string[] SubItems { get; set; }
        #endregion

        public EnterBlockEventArgs() : base() { }

        public EnterBlockEventArgs(bool Cancel) : base(Cancel) { }
    }

    /// <summary>
    /// Stellt Daten bereit wenn der XMLParser einen Block verläßt.
    /// </summary>
    public class LeaveBlockEventArgs : CancelWithMessageEventArgs
    {

        #region Property 
        public string Block { get; set; }
        #endregion

        public LeaveBlockEventArgs() : base() { }
        public LeaveBlockEventArgs(bool Cancel) : base(Cancel) { }
    }

    /// <summary>
    /// Stellt Daten bereit wenn der XMLParser einen reinen Datenblock erhalten hat.
    /// </summary>
    public class FetchItemEventArgs : CancelWithMessageEventArgs
    {

        #region Property 
        public string ItemName { get; set; }

        public string Value { get; set; }
        #endregion

        public FetchItemEventArgs() : base() { }

        public FetchItemEventArgs(bool Cancel) : base(Cancel) { }
    }

    /// <summary>
    /// Ein durch Ereignisse steuerbarer Parser welcher einfache XML-Dokumente verarbeiten kann.
    /// Attribute werden aktuell noch nicht unterstützt.
    /// </summary>
    public class XmlHelper : IDisposable
    {
        public EventHandler<EnterBlockEventArgs> EnterBlock;
        public EventHandler<LeaveBlockEventArgs> LeaveBlock;

        public EventHandler<FetchItemEventArgs> FetchItem;

        public void Parse(string DocumentType, string Value)
        {
            bool _inDoc = false, _fetchItem = false;
            string CurrentNode = "";

            Stack<EnterBlockEventArgs> Blocks = new Stack<EnterBlockEventArgs>();
            EnterBlockEventArgs CurrentBlock = null;

            // walk through
            using (XmlReader r = XmlReader.Create(new StringReader(Value)))
                while (r.Read())
                    switch (r.NodeType)
                    {
                        case XmlNodeType.Element:
                            // start doc, get layout
                            if (r.Name.Equals(DocumentType, StringComparison.OrdinalIgnoreCase))
                            {
                                _inDoc = true;
                                EnterBlock.Invoke(this, CurrentBlock = new EnterBlockEventArgs() { BlockName = r.Name, History = "" });
                                if (CurrentBlock.Cancel)
                                    throw new Exception(CurrentBlock.Message);
                            }
                            else if (_inDoc)
                            {
                                // block | item
                                if (CurrentBlock.SubBlocks != null)
                                    if (CurrentBlock.SubBlocks.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                                    {
                                        // block, enqueue current and get layout
                                        Blocks.Push(CurrentBlock);
                                        EnterBlock.Invoke(this, CurrentBlock = new EnterBlockEventArgs()
                                        {
                                            BlockName = r.Name,
                                            // quote history if equal items exists in diff. layers
                                            History = string.Join(".", Blocks.Select(x => x.BlockName))
                                        });

                                        if (CurrentBlock.Cancel)
                                            throw new Exception(CurrentBlock.Message);
                                    }

                                if (CurrentBlock.SubItems != null)
                                    if (CurrentBlock.SubItems.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                                        _fetchItem = true;

                                // need to name value in fetchitem event
                                CurrentNode = r.Name;
                            }
                            else // unknown state, throw 
                                throw new Exception("unexpected Node " + r.Name);

                            break;
                        case XmlNodeType.Text:
                            if (_fetchItem)
                            {
                                FetchItem?.Invoke(this, new FetchItemEventArgs()
                                {
                                    ItemName = CurrentNode,
                                    Value = r.Value
                                });
                                if (CurrentBlock.Cancel)
                                    throw new Exception(CurrentBlock.Message);
                            }
                            break;
                        case XmlNodeType.XmlDeclaration:
                            // coding 
                            break;
                        case XmlNodeType.EndElement:
                            if (r.Name.Equals(DocumentType, StringComparison.OrdinalIgnoreCase))
                            {
                                _inDoc = false;
                                return;
                            }
                            else if (_inDoc)
                            {
                                // end of current block (can by doc also) -> jump back
                                if (CurrentBlock.BlockName.Equals(r.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    CurrentBlock = Blocks.Pop();
                                    LeaveBlock?.Invoke(this, new LeaveBlockEventArgs()
                                    {
                                        Block = r.Name
                                    });
                                }

                                if (CurrentBlock.SubItems != null)
                                    if (CurrentBlock.SubItems.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                                        _fetchItem = false;

                                // end element -> clear 
                                CurrentNode = "";
                            }
                            else // unknown state, throw
                                throw new Exception("unexpected Node " + r.Name);

                            break;
                        case XmlNodeType.Whitespace:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }


                disposedValue = true;
            }
        }

        public void Dispose() =>
            Dispose(true);
        #endregion
    }
}