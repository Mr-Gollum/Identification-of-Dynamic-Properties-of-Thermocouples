#include <BluetoothSerial.h>
#include <Adafruit_ADS1X15.h>
#include <OneWire.h>
#include <Servo.h>

#define INTERVAL_1 1
#define INTERVAL_2 1000
#define INTERVAL_3 450
#define INTERVAL_4 1000
#define ADC_PIN_HOT 15 //hot temperature
#define ADC_PIN_COLD 2 //cold temperature
#define FANS 25	//Fan pin
#define HEATING 26	//heating element pin
#define servoPin 33	//Servo Pin
#define channel 2	//servo channel
OneWire ds1(4); // first DS18B20 on pin 4
OneWire ds2(5); // second DS18B20 on pin 5
byte data1[9], data2[9];	//adress for DS18B20 Do not change
byte addr1[8], addr2[8];

Servo servo1;
BluetoothSerial SerialBT;
Adafruit_ADS1115 ads;

unsigned long time_1 = 0, time_2 = 0, time_3 = 0, time_4 = 0;	// Do not change
float sensorVoltage, sensorTemperatureCold, sensorTemperatureHot, sensorVoltageOut, milliVolts,desiredTemperatureDifference;
int maxTemperature, desiredTemperature, operatingMode, numberOfmMeasurements;
bool startMeasuring = false, fans = false, coolDown=false, CanCoolDown=true;
bool moveUP=false, moveDOWN=false;

void setup() {
  Serial.begin(115200);		
  SerialBT.begin("ESP32_BT");	//BT device name
  
  pinMode(FANS, OUTPUT);
  pinMode(HEATING, OUTPUT);

  ledcSetup(channel, 5000, 8);	//Servo
  ledcAttachPin(HEATING,channel);

  // ads.setGain(GAIN_TWOTHIRDS);  // 2/3x gain +/- 6.144V  1 bit = 3mV      0.1875mV (default)
  ads.setGain(GAIN_ONE);        // 1x gain   +/- 4.096V  1 bit = 2mV      0.125mV
  // ads.setGain(GAIN_TWO);        // 2x gain   +/- 2.048V  1 bit = 1mV      0.0625mV
  // ads.setGain(GAIN_FOUR);       // 4x gain   +/- 1.024V  1 bit = 0.5mV    0.03125mV
  // ads.setGain(GAIN_EIGHT);      // 8x gain   +/- 0.512V  1 bit = 0.25mV   0.015625mV
  // ads.setGain(GAIN_SIXTEEN);    // 16x gain  +/- 0.256V  1 bit = 0.125mV  0.0078125mV
  ads.setDataRate(RATE_ADS1115_860SPS);
  
  
  servo1.attach(servoPin);

  if (!ads.begin()) {
    Serial.println("Failed to initialize ADS.");
    while (1);
  }
  
  Serial.println("The device started!");
  
}

void loop() {
  if (SerialBT.connected())  //If bluetooth is connected
  { 
    if (startMeasuring == 1)  //if user start measuring
    {
      if (millis() >= (time_1 + INTERVAL_1))  //activte every INTERVAL_1
      {
        time_1 += INTERVAL_1;
        MeasureVoltage();
      }
      if (millis() >= (time_2 + INTERVAL_2)) //activte every INTERVAL_2
      { 
        time_2 += INTERVAL_2;
        Serial.print("T2: ");
        Serial.print(time_2);
        Serial.print("   millis");
        Serial.println(millis());
        calculate();
        Heating();
        SendData();
        MeasureTemperature();
        CheckOperatingMode();
      }
    }
    else if (coolDown == true)
    { 
      if (sensorTemperatureCold > 30 || sensorTemperatureHot > 45) digitalWrite(FANS, HIGH);
      else 
      {
        digitalWrite(FANS, LOW);
        coolDown =false;
      } 
    }
    if (millis() >= (time_3 + INTERVAL_3)) 
    {
      time_3 += INTERVAL_3;
      receiveCommand();
    }

    if (millis() >= (time_4 + INTERVAL_4)) 
    {
      time_4 += INTERVAL_4;
      move();
      
    }
  }
}

void MeasureVoltage() 
{
  int16_t adc3 = ads.readADC_SingleEnded(3); // raw value
  float volts = ads.computeVolts(adc3); //Convert
  milliVolts += volts*1000; //Volts to milivolts
  numberOfmMeasurements++;  //number of measurements
}

void calculate()  //calculate and remove random measurement error
{ 
  sensorVoltageOut=milliVolts;
  milliVolts=0;
  sensorVoltageOut /= numberOfmMeasurements; // Average value of voltage in one second
  if ((sensorVoltageOut <= 0.2 && sensorVoltageOut >= 0) || (sensorVoltageOut <= 0 && sensorVoltageOut >= -0.2)) sensorVoltageOut = 0; // if voltage = +-0,2 set 0
  numberOfmMeasurements=0;
}

void receiveCommand() { 
  
  if (SerialBT.available() > 0) //receive command without blocking or waiting
  {
    String receivedCommand = SerialBT.readString();
    processCommand(receivedCommand);
  }
}

void processCommand(String command) 
{
  Serial.println("Received command: " + command); 
  int numCommand = command.toInt();
  switch (numCommand) {
    case 1000:
      startMeasuring = !startMeasuring; // switch true to false and so on
      time_1 = time_2 = time_3 = time_4 = millis(); //set interval base
      if (startMeasuring== false) coolDown = true;
      break;
    case 1001 ... 1002:  //change mode
      operatingMode = numCommand - 1000;
      digitalWrite(FANS, LOW);
      if (operatingMode==1)
      {
        desiredTemperature= desiredTemperatureDifference;
      }
      else if (operatingMode==2)
      {
        CanCoolDown=true;
      }
      break;

    case 10000 ... 10999:  //max temperature
      maxTemperature = numCommand - 10000;
      break;

    case 11000 ... 11999:  //desired temperature or desired temperature difference
      desiredTemperature = numCommand - 11000;
      desiredTemperatureDifference = numCommand - 11000;
      CanCoolDown=true;
      break;

    case 12000 ... 12002:  //Fan controll
      TurnOnOffFans();
      coolDown =false;
      CanCoolDown=false;
      break;

    case 13000 ... 13002:  //Move servo
      time_4 = millis();
      if (numCommand==13001) moveDOWN=true;
      else if (numCommand==13002) moveUP=true;
      break;
  }
}

void MeasureTemperature() //65ms
{ 
  //read first sensor
  if (!ds1.search(addr1)) ds1.reset_search();

  ds1.reset();
  ds1.select(addr1);
  ds1.write(0x44, 1); // convert
  ds1.reset();
  ds1.select(addr1);
  ds1.write(0xBE); // read Scratchpad

  for (byte i = 0; i < 9; i++) {
    data1[i] = ds1.read();
  }
  int16_t raw1 = (data1[1] << 8) | data1[0];
  float celsius1 = (float)raw1 / 16.0;
  // Read second sensor
  if (!ds2.search(addr2)) ds2.reset_search();

  ds2.reset();
  ds2.select(addr2);
  ds2.write(0x44, 1); // convert
  ds2.reset();
  ds2.select(addr2);
  ds2.write(0xBE); // read Scratchpad

  for (byte i = 0; i < 9; i++) {
    data2[i] = ds2.read();
  }
  int16_t raw2 = (data2[1] << 8) | data2[0];
  float celsius2 = (float)raw2 / 16.0;
  sensorTemperatureHot = celsius1;
  sensorTemperatureCold = celsius2;
}
void TurnOnOffFans()
{
  fans =!fans;
  digitalWrite(FANS, fans);
}
void Heating() //
{ 
  if (operatingMode==1)
  {
    if ((sensorTemperatureHot+13)<=(desiredTemperature)) {ledcWrite(channel, 255);} //Full power
    else if ((sensorTemperatureHot+5)<=(desiredTemperature)) {ledcWrite(channel, 40);}  //15,68% power if temp is 5°C from desired temp
    else if ((sensorTemperatureHot+3)<=(desiredTemperature)) {ledcWrite(channel, 30);}
    else if ((sensorTemperatureHot+1)<=(desiredTemperature)) {ledcWrite(channel, 25);}
    else if ((sensorTemperatureHot)<=(desiredTemperature)) {ledcWrite(channel, 20);}
    else if ((sensorTemperatureHot)>(desiredTemperature)) {ledcWrite(channel, 0); } //0% power

    if ((sensorTemperatureHot-2)>=(desiredTemperature))  //if temp is +2°C from desired
  {
    digitalWrite(FANS, HIGH);
    CanCoolDown=true;
  }
  else if (CanCoolDown==true) digitalWrite(FANS, LOW);

  }

  if (operatingMode==2)
  {
    if ((sensorTemperatureHot+13)<=(desiredTemperature)) {ledcWrite(channel, 255);}
    else if ((sensorTemperatureHot+5)<=(desiredTemperature)) {ledcWrite(channel, 80);}
    else if ((sensorTemperatureHot+3)<=(desiredTemperature)) {ledcWrite(channel, 60);}
    else if ((sensorTemperatureHot+1)<=(desiredTemperature)) {ledcWrite(channel, 50);}
    else if ((sensorTemperatureHot)<=(desiredTemperature)) {ledcWrite(channel, 40);}
    else if ((sensorTemperatureHot)>(desiredTemperature)) {ledcWrite(channel, 0); }

    if ((sensorTemperatureHot>60)&&(CanCoolDown==true)) 
    {
      coolDown=true;
    }
  }

  

}
void SendData()
{
  String SendMessage=(String(sensorVoltageOut) + "X" + String(sensorTemperatureHot) + "X" + String(sensorTemperatureCold) + "X" + String(desiredTemperature) + "X" + String(maxTemperature) + "Z");
  SerialBT.print(SendMessage);
}
void move() //move servo
{
  if (moveUP== true)
  {
    servo1.write(0);
    moveUP=false;
  }
  else if (moveDOWN == true)
  {
    servo1.write(180);
    moveDOWN=false;
  }
  else
  {
    servo1.write(90);
  }
}

void CheckOperatingMode()
{
  if (operatingMode==2)
  {
    desiredTemperature = sensorTemperatureCold + desiredTemperatureDifference+1;
    
  }
}
