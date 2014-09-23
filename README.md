Event Viewer for Azure Web Sites
===========================================

Every Azure Website has an Eventlog.xml file that is present in the D:\home\LogFiles folder of the web site. 

This Azure SiteExtension reads that EventLog.xml file and presents the event messages as if you are viewing them in the Event Viewer. It also allows you to filter events based on Source, EventId, and Level of the event. You can also search the Event Description easily by entering some text.

