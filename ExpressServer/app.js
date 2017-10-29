const express = require('express');
const https = require('https');
const bodyParser = require('body-parser');
const queryString = require('querystring');
const crypto = require('jsrsasign').KEYUTIL;
const keys = require('./keys.js');
const dirty = require('dirty');

const db = dirty('app.db');
const app = express();

app.use(bodyParser.json());

function server () {
  app.post('/register', function (req, res) {
    if (!req.body.mobile || !req.body.public_key)
      res.sendStatus(400);

    db.set(req.body.mobile, { 
      key: req.body.public_key,
      id: 96
    });

    console.log(
      "%s registering:\n%s", 
      req.body.mobile, 
      req.body.public_key);

    res.sendStatus(200);
  });

  app.post('/message', function (req, res) {
    if (!req.body.message) res.sendStatus(400);

    var ourKey = crypto.getKey(keys.privateKey);

    var pt = ourKey.decrypt(req.body.message);

    if (!pt) res.sendStatus(400);

    var components = pt.split(/~/);

    if (!components || components.length != 2) res.sendStatus(400);

    var recipient = db.get(components[0]);

    if (!recipient) res.sendStatus(400);

    var theirKey = crypto.getKey(recipient.key);

    var cypher = theirKey.encrypt(components[1]);

    var parts = cypher.match(/.{1,137}/g);

    var id = recipient.id;

    id++;

    if (id == 123) {
      id = 97;
    }

    db.rm(components[0]);

    db.set(components[0], {
      key: recipient.key,
      id: id
    });

    var sentCount = 0;
    for (var i = 0; i < parts.length; ++i) {
      var sendTextOptions = {
        host: 'api.clockworksms.com',
        path: '/http/send.aspx?'
      };

      var payload = String.fromCharCode(id).concat(i.toString(), parts[i]);

      sendTextOptions.path += queryString.stringify({
        key: encodeURIComponent(keys.clockworkApiKey),
        to: encodeURIComponent(components[0]),
        content: encodeURIComponent(payload)
      });

      https.get(sendTextOptions, (sendRes) => {
        console.log("Sent message");
        sentCount++;

        if (sentCount == parts.length)
          res.sendStatus(200)

      }).on('error', (e) => {
        console.error(e);
      });
    }
  });

  app.listen(3000, function () {
    console.log('Clockwork Proxy listening on port 3000!');
  })
}

db.on('load', server);
