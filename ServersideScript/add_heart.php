<?php

header('Content-Type: application/json');

// Include database configuration
require_once 'config.php';

try {
    $conn = new PDO("mysql:host=$servername;dbname=$dbname", $dbusername, $dbpassword);
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    echo json_encode(["status" => "error", "message" => "Database connection failed: " . $e->getMessage()]);
    exit;
}

// Check if data is received via POST
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    // Get user_id and heart_change from POST
    $user_id = isset($_POST['user_id']) ? (int)$_POST['user_id'] : 0;
    $heart_change = isset($_POST['heart_change']) ? (int)$_POST['heart_change'] : 0;

    if ($user_id <= 0 || $heart_change === 0) {
        echo json_encode(["status" => "error", "message" => "invalidinput"]);
        exit;
    }

    // Fetch current diamond and heart values from user_data
    $stmt = $conn->prepare("
        SELECT u.user_id, u.username, ud.diamond, ud.heart
        FROM user u
        INNER JOIN user_data ud ON u.user_id = ud.user_id
        WHERE u.user_id = :user_id
    ");
    $stmt->execute(['user_id' => $user_id]);

    if ($user = $stmt->fetch(PDO::FETCH_ASSOC)) {
        // Calculate new heart value
        $new_heart = $user['heart'] + $heart_change;

        // Check if the new heart value is within the valid range
        if ($new_heart < 0) {
            echo json_encode(["status" => "error", "message" => "heartlessthanzero"]);
            exit;
        } elseif ($new_heart > 100) {
            echo json_encode(["status" => "error", "message" => "heartbeyondmaxvalue"]);
            exit;
        }

        // Update the heart value in the database
        $stmt = $conn->prepare("UPDATE user_data SET heart = :heart WHERE user_id = :user_id");
        $stmt->execute(['heart' => $new_heart, 'user_id' => $user_id]);

        // Return updated user data
        echo json_encode([
            "status" => "success",
            "user_id" => $user['user_id'],
            "username" => $user['username'],
            "diamond" => $user['diamond'],
            "heart" => $new_heart
        ]);
    } else {
        echo json_encode(["status" => "error", "message" => "usernotfound"]);
    }
} else {
    echo json_encode(["status" => "error", "message" => "Invalid request method."]);
}

?>
