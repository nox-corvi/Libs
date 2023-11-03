# Libs

Signatures

Signature1:
0x0xDFA1 BITS

Signature2:

BITS 0xFCAx Net.Com.MessageTypeEnum
BITS 0xFCBx Net.Com.SecureMessageTypeEnum

Signature3 (can be the same as Signature2):
BITS 0xFAxx BitsCommon.Net.CommonMessageTypeEnum (CRAW)
BITS 0xFBxx BitsClient.Net.ClientMessageTypeEnum
BITS 0xFCxx BitsServer.Net.ServerMessageTypeEnum

BITS 0xFDxx BitsClient.Net.ClientControlMessageTypeEnum
BITS 0xFExx BitsServer.Net.ServerControlMessageTypeEnum

BITS CSCommand (Client / Server Commands) 
BITS CCCommand (Client Control Commands)
BITS SCCommand (Server Control Commands)

Ports
BITS #7855 Server 
BITS #7854 Client Control
BITS #7853 Server Control ?

GRID #7865 Operator Service
GRID #7864 Simulator Service
GRID #7863 Visualizer Service