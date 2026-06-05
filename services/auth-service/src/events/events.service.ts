import { Injectable, OnModuleInit, OnModuleDestroy, Logger } from '@nestjs/common';
import * as amqp from 'amqplib';

@Injectable()
export class EventsService implements OnModuleInit, OnModuleDestroy {
  private readonly logger = new Logger(EventsService.name);
  private connection: any;
  private channel: any;
  private readonly exchange = 'auth.events';

  async onModuleInit() {
    await this.connect();
  }

  async onModuleDestroy() {
    if (this.channel) await this.channel.close();
    if (this.connection) await this.connection.close();
  }

  private async connect() {
    try {
      const url = process.env.RABBITMQ_URL || 'amqp://localhost:5672';
      this.connection = await amqp.connect(url);
      this.channel = await this.connection.createChannel();
      
      // Assert a topic exchange for auth events
      await this.channel.assertExchange(this.exchange, 'topic', { durable: true });
      this.logger.log(`Connected to RabbitMQ at ${url}`);
    } catch (error) {
      this.logger.error('Failed to connect to RabbitMQ', error);
      // In a real app, we might retry connection
    }
  }

  async emitEvent(routingKey: string, payload: any) {
    if (!this.channel) {
      this.logger.warn(`Cannot emit event ${routingKey}: channel not established.`);
      return;
    }
    
    const message = Buffer.from(JSON.stringify(payload));
    this.channel.publish(this.exchange, routingKey, message, {
      persistent: true,
      timestamp: Date.now()
    });
    this.logger.debug(`Emitted event: ${routingKey}`);
  }
}
