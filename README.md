# Libs

# Net.Com
#	Signatures
#		Signature1:
		0x0xDFA1 BITS

#		Signature2:

		BITS 0xFCAx Net.Com.MessageTypeEnum
		BITS 0xFCBx Net.Com.SecureMessageTypeEnum

#		Signature3 (can be the same as Signature2):
		BITS 0xFAxx BitsCommon.Net.CommonMessageTypeEnum (CRAW)
		BITS 0xFBxx BitsClient.Net.ClientMessageTypeEnum
		BITS 0xFCxx BitsServer.Net.ServerMessageTypeEnum

		BITS 0xFDxx BitsClient.Net.ClientControlMessageTypeEnum
		BITS 0xFExx BitsServer.Net.ServerControlMessageTypeEnum

		BITS CSCommand (Client / Server Commands) 
		BITS CCCommand (Client Control Commands)
		BITS SCCommand (Server Control Commands)

#		Ports
		BITS #7855 Server 
		BITS #7854 Client Control
		BITS #7853 Server Control ?

		GRID #7865 Operator Service
		GRID #7875 Operator 
		GRID #7864 Simulator Service
		GRID #7863 Visualizer Service



# WebApi - ResponseShell Bibliothek
	Eine erweiterte Bibliothek für die Anbindung von Daten an ein
	statusbasiertes, serialisierbares Rückgabeobjekt mit zusätzlichen
	Datenfeldern. Optimiert für RESTful-Apis und erweitert die
	Grundfunktionalität der Shell-Klasse um die Handhabung von zusätzlichen
	Daten.

	# StateEnum
		- Success: Die Abfrage war erfolgreich.
		- Failure: Die Abfrage ist fehlgeschlagen, z.B. wurden keine Daten
			gefunden. Die Ausführung war ansonsten aber fehlerfrei.
		- Error: Die Ausführung ist fehlgeschlagen, eine Ausnahme oder ein
			Fehler ist aufgetreten.

	# ResponseShell
		Eine Basis-Klasse zur Kapselung von Status und Nachricht, erweitert
		um die Fähigkeit, zusätzliche Daten zu speichern. Dient als
		Grundlage für spezialisierte Rückgabeobjekte.
		
		# DefaultHandler Methode
			Behandelt eine ResponseShell basierend auf ihrem Zustand und führt bei
			Erfolg eine gegebene Funktion aus. Diese Methode prüft den Zustand der
			übergebenen ResponseShell und führt, falls dieser 'Success' ist, die
			übergebene onSuccess-Funktion aus. Bei einem Zustand 'Failure' oder 'Error'
			wird eine neue ResponseShell mit einer entsprechenden Fehlermeldung erzeugt
			und zurückgegeben.

	# SingleDataResponseShell
		Erweitert ResponseShell um die Handhabung eines einzelnen
		zusätzlichen Datenfeldes.

		# Eigenschaften
			- AdditionalData1: Ein einzelnes zusätzliches Datenfeld für
				erweiterte Antwortdaten.

	# DoubleDataResponseShell
		Erweitert SingleDataResponseShell um die Handhabung von zwei
		zusätzlichen Datenfeldern.

		# Eigenschaften
			- AdditionalData2: Ein zweites zusätzliches Datenfeld für
				erweiterte Antwortdaten.

	# QuadDataResponseShell
		Erweitert DoubleDataResponseShell um die Handhabung von vier
		zusätzlichen Datenfeldern.

		# Eigenschaften
			- AdditionalData3: Ein drittes zusätzliches Datenfeld für
				erweiterte Antwortdaten.
			- AdditionalData4: Ein viertes zusätzliches Datenfeld für
				erweiterte Antwortdaten.
