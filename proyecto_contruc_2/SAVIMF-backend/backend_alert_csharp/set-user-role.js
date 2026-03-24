/**
 * Script para asignar roles (admin/operador) a usuarios de Firebase
 * 
 * Uso:
 *   node set-user-role.js <email> <role>
 * 
 * Ejemplos:
 *   node set-user-role.js admin@savimf.com admin
 *   node set-user-role.js operador@savimf.com operador
 */

const admin = require('firebase-admin');

// Inicializar Firebase Admin SDK
const serviceAccount = require('./backend_alert/savimf-alert-firebase-adminsdk-key.json');

admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

async function setUserRole(email, role) {
  try {
    // Validar rol
    if (!['admin', 'operador'].includes(role)) {
      console.error('❌ Rol inválido. Use "admin" o "operador"');
      process.exit(1);
    }

    // Buscar usuario por email
    const user = await admin.auth().getUserByEmail(email);
    
    // Asignar custom claim
    await admin.auth().setCustomUserClaims(user.uid, { role });
    
    console.log(`✅ Rol "${role}" asignado a ${email} (UID: ${user.uid})`);
    console.log(`⚠️  El usuario debe cerrar sesión y volver a iniciar para que el cambio tenga efecto.`);
    
    process.exit(0);
  } catch (error) {
    console.error('❌ Error:', error.message);
    process.exit(1);
  }
}

// Obtener argumentos de línea de comandos
const email = process.argv[2];
const role = process.argv[3];

if (!email || !role) {
  console.log('Uso: node set-user-role.js <email> <role>');
  console.log('Roles disponibles: admin, operador');
  console.log('\nEjemplos:');
  console.log('  node set-user-role.js admin@savimf.com admin');
  console.log('  node set-user-role.js operador@savimf.com operador');
  process.exit(1);
}

setUserRole(email, role);
