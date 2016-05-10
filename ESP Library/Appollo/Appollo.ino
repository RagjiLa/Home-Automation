#include <ESP8266HTTPClient.h>
#include "DHT.h"
#include <ESP8266WiFi.h>


#define DHTPIN 2
#define DHTTYPE DHT22

DHT dht(DHTPIN, DHTTYPE, 15);

String response;


void ConnectToWifi()
{
  WiFi.hostname("Temperature" + String(ESP.getChipId()));
  WiFi.mode(WIFI_STA);

  byte powerUsageCtr = 0;
  byte maximumPowerCycle = 20;

  char ssid2[] = "Blackknife_EXT";
  char password2[] = "19871116";
  powerUsageCtr = 0;
  while (powerUsageCtr <= maximumPowerCycle)
  {
    if ( WiFi.begin(ssid2, password2) == WL_CONNECTED) return;
    delay(2000);
    if (WiFi.status() == WL_CONNECTED)
    {
      Serial.println("Connected");
      return;
    }
    Serial.print("Connecting ");
    Serial.println(powerUsageCtr);
    powerUsageCtr++;
    digitalWrite(12, !digitalRead(12));
  }
  WiFi.disconnect();

  delay(1000);

  digitalWrite(12, LOW);
  ESP.deepSleep(60000000, WAKE_RFCAL);
  delay(1000);
}

void setup()
{
  pinMode(12, OUTPUT);//Green
  pinMode(13, OUTPUT);//Blue
  digitalWrite(12, HIGH);

  dht.begin();

  ConnectToWifi();

  digitalWrite(12, LOW);
  Serial.begin(115200);
}

void loop()
{
  digitalWrite(13, HIGH);

  float h = dht.readHumidity();
  float t = dht.readTemperature();
  float hic = dht.computeHeatIndex(t, h, false);

  if (isnan(h) || isnan(t) || isnan(hic))
  {

  }
  else
  {
    Serial.println("Trying to upload");
    HTTPClient http;
    http.begin("http://192.168.1.13:9000/Temperature/" + String(ESP.getChipId()));
    http.addHeader("Content-Type", "application/json");
    http.addHeader("Accept", "application/json");
    String payload = "{";
    payload += "\"Temperature\":\"" + String(t) + "\",";
    payload += "\"Humidity\":\"" + String(h) + "\",";
    payload += "\"HeatIndex\":\"" + String(hic) + "\",";
    payload += "\"RSSI\":\"" + String(WiFi.RSSI()) + "\",";
    payload += "\"WakeTime\":\"" + String(millis()) + "\"";
    payload += "}";
    int httpCode = http.POST(payload);
    Serial.println(httpCode);
    http.writeToStream(&Serial);
    http.end();
  }

  delay(1000);
  WiFi.disconnect();
  delay(1000);
  digitalWrite(13, LOW);
  ESP.deepSleep(60000000, WAKE_RF_DEFAULT);
  delay(1000);
}

