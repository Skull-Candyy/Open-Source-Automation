CALL osae_sp_object_type_add ('THERMOSTAT','Thermostat','','THING',0,1,0,1,'Thermostat.
This can be a wired or Wifi enabled Thermostat.');
CALL osae_sp_object_type_state_add('THERMOSTAT','HEAT ON','Heat On','This state represents that this thermostat Cool is Off.');
CALL osae_sp_object_type_state_add('THERMOSTAT','OFF','Off','This state represents that this thermostat is currently Off.');
CALL osae_sp_object_type_state_add('THERMOSTAT','COOL ON','Cool On','This state represents that this thermostat Cool is On.');
CALL osae_sp_object_type_event_add('THERMOSTAT','COOL ON','Cool On','This event will fire when this Thermostat state changes to Cool-On.');
CALL osae_sp_object_type_event_add('THERMOSTAT','HEAT ON','Heat On','This event will fire when this Thermostat state changes to Heat-On.');
CALL osae_sp_object_type_event_add('THERMOSTAT','FAN ON','Fan On','This event will fire when this Thermostat Fan changes to on.');
CALL osae_sp_object_type_event_add('THERMOSTAT','FAN OFF','Fan Off','This event will fire when this Thermostat Fan changes to off.');
CALL osae_sp_object_type_event_add('THERMOSTAT','OFF','Off','This event will fire when this Thermostat state changes to Off.');
CALL osae_sp_object_type_event_add('THERMOSTAT','TEMPERATURE','Tempurature','This event will fire when this Thermostat Temperature changes.');
CALL osae_sp_object_type_event_add('THERMOSTAT','HEAT SP CHANGED','Heat Setpoint','This event will fire when this Thermostat Cool Setpoint changes.');
CALL osae_sp_object_type_event_add('THERMOSTAT','COOL SP CHANGED','Cool Setpoint','This event will fire when this Thermostat Cool Setpoint changes.');
CALL osae_sp_object_type_event_add('THERMOSTAT','FAN MODE CHANGED','Fan Mode','This event will fire when this Fan Mode changes.');
CALL osae_sp_object_type_method_add('THERMOSTAT','HEAT','Heat','','','','','This state represents that this thermostat is currently set to Heat.');
CALL osae_sp_object_type_method_add('THERMOSTAT','AUTO','Auto','','','','','Executing this method will set this Thermostat to Auto.');
CALL osae_sp_object_type_method_add('THERMOSTAT','COOL','Cool','','','','','Executing this method will set this Thermostat to Cool.');
CALL osae_sp_object_type_method_add('THERMOSTAT','FAN AUTO','Fan Auto','','','','','Set this Thermostat\'s Fan to Auto.');
CALL osae_sp_object_type_method_add('THERMOSTAT','FAN ON','Fan On','','','','','Set this Thermostat\'s Fan to On.');
CALL osae_sp_object_type_method_add('THERMOSTAT','FAN OFF','Fan Off','','','','','Set this Thermostat\'s Fan to Off.');
CALL osae_sp_object_type_method_add('THERMOSTAT','HEATSP','Heat Setpoint','Setpoint','','','','Set this Thermostat\'s Heat Setpoint to Parameter 1.');
CALL osae_sp_object_type_method_add('THERMOSTAT','COOLSP','Cool Setpoint','Setpoint','','','','Set this Thermostat\'s Cool Setpoint to Parameter 1.');
CALL osae_sp_object_type_method_add('THERMOSTAT','OFF','Off','','','','','Set this Thermostat to Off.');
CALL osae_sp_object_type_property_add('THERMOSTAT','Heat Setpoint','Integer','','0',0,0,'Currently set Heat Setpoint.');
CALL osae_sp_object_type_property_add('THERMOSTAT','Cool Setpoint','Integer','','0',0,0,'Currently set Cool Setpoint.');
CALL osae_sp_object_type_property_add('THERMOSTAT','Current Temperature','Integer','','0',0,0,'Currently Temperature.');
CALL osae_sp_object_type_property_add('THERMOSTAT','Operating Mode','String','','',0,0,'Current Operating Mode.');
CALL osae_sp_object_type_property_option_add('THERMOSTAT','Operating Mode','Heat');
CALL osae_sp_object_type_property_option_add('THERMOSTAT','Operating Mode','Cool');
CALL osae_sp_object_type_property_option_add('THERMOSTAT','Operating Mode','Auto');
CALL osae_sp_object_type_property_option_add('THERMOSTAT','Operating Mode','Off');
CALL osae_sp_object_type_property_add('THERMOSTAT','Fan Mode','String','','',0,0,'Currently set Fan Mode.');
CALL osae_sp_object_type_property_option_add('THERMOSTAT','Fan Mode','Auto');
CALL osae_sp_object_type_property_option_add('THERMOSTAT','Fan Mode','On');

CALL osae_sp_object_type_add ('APRILAIRETHERMOSTAT','Aprilaire Thermostat','','PLUGIN',1,0,0,1,'Aprilaire Thermostat');
CALL osae_sp_object_type_state_add ('APRILAIRETHERMOSTAT','ON','Running','Aprilaire Thermostat is Running');
CALL osae_sp_object_type_state_add ('APRILAIRETHERMOSTAT','OFF','Stopped','Aprilaire Thermostat is Stopped');
CALL osae_sp_object_type_event_add ('APRILAIRETHERMOSTAT','ON','Started','Aprilaire Thermostat Started');
CALL osae_sp_object_type_event_add ('APRILAIRETHERMOSTAT','OFF','Stopped','Aprilaire Thermostat Stopped');
CALL osae_sp_object_type_method_add ('APRILAIRETHERMOSTAT','ON','Start','','','','','Start the Aprilaire Thermostat');
CALL osae_sp_object_type_method_add ('APRILAIRETHERMOSTAT','OFF','Stop','','','','','Stop the Aprilaire Thermostat');
CALL osae_sp_object_type_property_add ('APRILAIRETHERMOSTAT','IP Address','String','','',0,1,'IP Address');
CALL osae_sp_object_type_property_add ('APRILAIRETHERMOSTAT','Port','Integer','','0',0,1,'Port');
CALL osae_sp_object_type_property_add('INSTEON','Version','String','','',0,1,'Version of the Aprilaire Thermostat plugin');
CALL osae_sp_object_type_property_add('INSTEON','Author','String','','',0,1,'Author of the Aprilaire Thermostat plugin');
CALL osae_sp_object_type_property_add('INSTEON','Trust Level','Integer','','90',0,1,'Aprilaire Thermostat plugin Trust Level');