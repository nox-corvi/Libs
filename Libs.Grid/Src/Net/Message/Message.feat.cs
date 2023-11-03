using Nox.Net.Com.Message;
using Nox.Net.Com;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Grid.Net.Message;

public enum FeatureEnum
{

}

public class MessageFeatData
	: Nox.Net.Com.Message.MessageFeatData<FeatureEnum>
{
	public MessageFeatData(uint Signature2)
		: base(Signature2) { }
}

public class MessageFeat
   : MessageFeat<FeatureEnum>
{
	public MessageFeat(uint Signature1)
		: base(Signature1)  { }
}
