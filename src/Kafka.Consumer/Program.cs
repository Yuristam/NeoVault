using Confluent.Kafka;

var kafkaConsumerConfig = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "reader-1",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var kafkaConsumer = new ConsumerBuilder<string, string>
    (kafkaConsumerConfig).Build();
kafkaConsumer.Subscribe("chat");

while (true)
{
    var consumeResult = kafkaConsumer.Consume(TimeSpan.FromSeconds(1));
    if (consumeResult is null) continue;

    Console.WriteLine($"Прочитано (partition {consumeResult.Partition.Value}, " +
        $"offset {consumeResult.Offset.Value}): {consumeResult.Message.Value}");
}