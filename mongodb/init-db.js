db = db.getSiblingDB("eloja");

db.createCollection("ordens");

db.createUser({
  user: "dev",
  pwd: "dev",
  roles: [{ role: "readWrite", db: "eloja" }],
});
