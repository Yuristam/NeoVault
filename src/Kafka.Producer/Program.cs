using Confluent.Kafka;

var kafkaProducerConfig = new ProducerConfig
{
    BootstrapServers = "localhost:9092"
};

using var kafkaProducer = new ProducerBuilder<string, string>
(kafkaProducerConfig).Build();


Console.WriteLine("Пиши строки - они уедет в Kafka. Пустая строка = выход.");

while(Console.ReadLine() is { } line && line != "")
{
    var deliveryReport = await kafkaProducer.ProduceAsync(
        "chat",
        new Message<string, string> { Key = "user", Value = line });

    Console.WriteLine($"Отправлено: partition {deliveryReport.Partition.Value}," +
        $"offset {deliveryReport.Offset.Value}");
}