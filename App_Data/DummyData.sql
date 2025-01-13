-- Insert into Application table
INSERT INTO Application (name, creation_datetime) VALUES 
('App1', SYSDATETIME()), 
('App2', SYSDATETIME()),
('App3', SYSDATETIME()),
('App4', SYSDATETIME());
-- Insert into Container table
INSERT INTO Container (name, creation_datetime, parent) VALUES 
('Container1', SYSDATETIME(), 1), 
('Container2', SYSDATETIME(), 1), 
('Container3', SYSDATETIME(), 2);

-- Insert into Record table
INSERT INTO Record (name, content, creation_datetime, parent) VALUES 
('Record1', 'Content of Record1', SYSDATETIME(), 1), 
('Record2', 'Content of Record2', SYSDATETIME(), 1), 
('Record3', 'Content of Record3', SYSDATETIME(), 2);

-- Insert into Notification table
INSERT INTO Notification (name, creation_datetime, parent, event, endpoint, enabled) VALUES 
('Notification1a', SYSDATETIME(), 1, 1, 'http://localhost:5000/test/api/notification', 1), 
('Notification1b', SYSDATETIME(), 2, 1, 'http://localhost:5000/test/api/notification', 1), 
('Notification2a', SYSDATETIME(), 1, 1, 'mqtt://127.0.0.1', 1), 
('Notification2b', SYSDATETIME(), 2, 1, 'mqtt://127.0.0.1', 1), 
('Notification3', SYSDATETIME(), 2, 1, 'http://example.com/endpoint3', 0);
